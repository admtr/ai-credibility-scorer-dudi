using DuDi.Core.Registry;
using DuDi.Core.Risk;

namespace DuDi.Core.Verification;

/// <summary>
/// Zbiera fakty o kontrahencie ze wszystkich rejestrów i liczy ocenę ryzyka.
/// Łańcuch: NIP → Biała lista VAT → numer KRS → odpis KRS → CRBR, listy sankcyjne, powiązania osobowe.
/// </summary>
public sealed class CounterpartyVerifier(IRegistrySource source, TimeProvider timeProvider)
{
    public Task<RiskReport> VerifyAsync(Nip nip, BankAccountNumber? invoiceAccount = null, CancellationToken cancellationToken = default) =>
        VerifyAsync(nip, invoiceAccount, checkRelations: true, cancellationToken);

    private async Task<RiskReport> VerifyAsync(Nip nip, BankAccountNumber? invoiceAccount, bool checkRelations, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        var vat = await CheckAsync(SourceNames.WhiteList, today, () => source.GetVatSubjectAsync(nip, today, ct), ct);

        SourceCheck<AccountCheck>? account = null;

        if (invoiceAccount is { } invoice)
        {
            account = await CheckAsync<AccountCheck>(SourceNames.WhiteList, today,
                async () => new AccountCheck(invoice, await source.IsAccountAssignedAsync(nip, invoice, today, ct)), ct);
        }

        var krs = await CheckKrsAsync(vat, today, ct);
        var name = vat.Value?.Name ?? krs.Value?.Name;

        if (vat.Status != CheckStatus.Ok && krs.Status != CheckStatus.Ok)
        {
            // Nie wiemy nawet, jak firma się nazywa – pozostałych rejestrów nie da się sensownie odpytać.
            var notApplicable = "nie ustalono danych podmiotu";
            var unknown = new CompanyProfile(nip, name, today, vat, account, krs,
                NotApplicable<BeneficialOwnersInfo>(SourceNames.Crbr, today, notApplicable),
                NotApplicable<SanctionsCheck>(SourceNames.Sanctions, today, notApplicable),
                NotApplicable<RelationsCheck>(SourceNames.Relations, today, notApplicable));

            return RiskScorer.Score(unknown);
        }

        var beneficialOwners = krs.Status == CheckStatus.NotApplicable
            ? NotApplicable<BeneficialOwnersInfo>(SourceNames.Crbr, today, "CRBR dotyczy spółek wpisanych do KRS")
            : await CheckAsync(SourceNames.Crbr, today, () => source.GetBeneficialOwnersAsync(nip, ct), ct);

        var sanctions = await CheckSanctionsAsync(name, krs.Value, beneficialOwners.Value, today, ct);

        var relations = checkRelations
            ? await CheckRelationsAsync(nip, krs, today, ct)
            : NotApplicable<RelationsCheck>(SourceNames.Relations, today, null);

        var profile = new CompanyProfile(nip, name, today, vat, account, krs, beneficialOwners, sanctions, relations);
        return RiskScorer.Score(profile);
    }

    private async Task<SourceCheck<KrsEntry>> CheckKrsAsync(SourceCheck<VatSubject> vat, DateOnly today, CancellationToken ct)
    {
        if (vat.Status == CheckStatus.Unavailable)
        {
            return new SourceCheck<KrsEntry>(SourceNames.Krs, today, CheckStatus.Unavailable,
                Note: "nie ustalono numeru KRS, bo Biała lista VAT nie odpowiedziała");
        }

        if (vat.Value?.KrsNumber is not { Length: > 0 } krsNumber)
            return NotApplicable<KrsEntry>(SourceNames.Krs, today, "podmiot nie ma numeru KRS na Białej liście (np. jednoosobowa działalność) – KRS nie dotyczy");

        return await CheckAsync(SourceNames.Krs, today, () => source.GetKrsEntryAsync(krsNumber, ct), ct);
    }

    private async Task<SourceCheck<SanctionsCheck>> CheckSanctionsAsync(
        string? companyName, KrsEntry? krs, BeneficialOwnersInfo? owners, DateOnly today, CancellationToken ct)
    {
        // Sprawdzamy firmę, zarząd i beneficjentów. Zamaskowanych nazwisk z publicznego KRS nie da się porównać.
        var names = new List<string>();

        if (companyName is not null)
            names.Add(companyName);

        if (krs is not null)
            names.AddRange(krs.Board.Where(m => !m.IsMasked).Select(m => m.FullName));

        if (owners is not null)
            names.AddRange(owners.Owners.Select(o => o.FullName));

        names = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (names.Count == 0)
            return NotApplicable<SanctionsCheck>(SourceNames.Sanctions, today, "brak nazw do sprawdzenia");

        return await CheckAsync<SanctionsCheck>(SourceNames.Sanctions, today, async () =>
        {
            var hits = new List<SanctionHit>();

            foreach (var name in names)
                hits.AddRange(await source.CheckSanctionsAsync(name, ct));

            return new SanctionsCheck(names, hits);
        }, ct);
    }

    private async Task<SourceCheck<RelationsCheck>> CheckRelationsAsync(Nip nip, SourceCheck<KrsEntry> krs, DateOnly today, CancellationToken ct)
    {
        if (krs.Value is not { } entry)
            return NotApplicable<RelationsCheck>(SourceNames.Relations, today, null);

        var people = entry.Board.Where(m => !m.IsMasked).ToList();

        if (people.Count == 0)
        {
            return NotApplicable<RelationsCheck>(SourceNames.Relations, today, entry.Board.Count > 0
                ? "dane członków zarządu w KRS są zamaskowane – powiązań osobowych nie sprawdzono"
                : null);
        }

        return await CheckAsync<RelationsCheck>(SourceNames.Relations, today, async () =>
        {
            var related = new List<RelatedCompany>();
            var seen = new HashSet<Nip> { nip };

            foreach (var person in people)
            {
                foreach (var link in await source.FindCompaniesByPersonAsync(person.FullName, ct))
                {
                    if (!seen.Add(link.Nip))
                        continue;

                    // Jeden poziom w głąb: powiązanej firmy nie sprawdzamy już pod kątem jej własnych powiązań.
                    var linkedReport = await VerifyAsync(link.Nip, null, checkRelations: false, ct);
                    related.Add(new RelatedCompany(link, linkedReport.Verdict));
                }
            }

            return new RelationsCheck(related);
        }, ct);
    }

    private static async Task<SourceCheck<T>> CheckAsync<T>(string sourceName, DateOnly today, Func<Task<T?>> fetch, CancellationToken ct)
        where T : class
    {
        try
        {
            var value = await fetch();

            return value is null
                ? new SourceCheck<T>(sourceName, today, CheckStatus.NotFound)
                : new SourceCheck<T>(sourceName, today, CheckStatus.Ok, value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Każdy błąd źródła to „nie udało się sprawdzić”, nigdy „wszystko OK”.
            return new SourceCheck<T>(sourceName, today, CheckStatus.Unavailable, Note: ex.Message);
        }
    }

    private static SourceCheck<T> NotApplicable<T>(string sourceName, DateOnly today, string? note) where T : class =>
        new(sourceName, today, CheckStatus.NotApplicable, Note: note);
}
