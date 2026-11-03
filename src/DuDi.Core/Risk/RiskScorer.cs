using System.Globalization;
using DuDi.Core.Registry;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk;

/// <summary>
/// Reguły oceny ryzyka v0. Werdykt to najwyższa waga wśród ustaleń (brak ustaleń = ryzyko niskie).
/// </summary>
/// <remarks>
/// Ocena jest liczona w kodzie, a nie przez LLM: wynik jest powtarzalny, testowalny i łatwo pokazać, skąd się wziął.
/// Źródło, które nie odpowiedziało, nie generuje ustalenia – trafia na listę „nie sprawdzono”.
/// </remarks>
public static class RiskScorer
{
    public static RiskReport Score(CompanyProfile profile)
    {
        var findings = new List<Finding>();
        var checks = new List<CheckSummary>();

        if (!profile.IsKnown)
        {
            checks.Add(profile.Vat.Status == CheckStatus.Unavailable
                ? NotChecked(profile.Vat, "nie udało się ustalić, czy podmiot figuruje na Białej liście")
                : new CheckSummary(profile.Vat.Source, profile.Vat.CheckedOn, CheckOutcome.Info,
                    "brak podmiotu o tym NIP w wykazie podatników VAT; bez numeru KRS nie da się sprawdzić pozostałych rejestrów"));

            return new RiskReport(profile, null, [], checks);
        }

        ScoreVat(profile, findings, checks);
        ScoreAccount(profile, findings, checks);
        ScoreKrs(profile, findings, checks);
        ScoreCompanyAge(profile, findings, checks);
        ScoreBeneficialOwners(profile, findings, checks);
        ScoreSanctions(profile, findings, checks);
        ScoreRelations(profile, findings, checks);

        var ordered = findings.OrderByDescending(f => f.Severity).ToList();
        var verdict = ordered.Count == 0 ? RiskLevel.Low : ordered[0].Severity;

        return new RiskReport(profile, verdict, ordered, checks);
    }

    /// <summary>
    /// Ostatni rok obrotowy, za który sprawozdanie powinno już być w KRS. Sprawozdanie za rok N zatwierdza się
    /// do 30 czerwca N+1 i składa w ciągu 15 dni, więc przyjmujemy termin 15 lipca. Zakładamy rok obrotowy = kalendarzowy.
    /// </summary>
    public static int LatestRequiredFiscalYear(DateOnly today)
    {
        var deadline = new DateOnly(today.Year, 7, 15);
        return today > deadline ? today.Year - 1 : today.Year - 2;
    }

    private static void ScoreVat(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        var vat = profile.Vat;

        switch (vat.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(NotChecked(vat, "status VAT i rachunki bankowe"));
                return;

            case CheckStatus.NotFound:
                findings.Add(new Finding(
                    FindingCodes.VatNotFound,
                    RiskLevel.High,
                    "Podmiotu nie ma w wykazie podatników VAT (Biała lista).",
                    "Upewnij się, czy kontrahent może wystawić fakturę z VAT. W razie wątpliwości skonsultuj się z księgowością przed zapłatą.",
                    vat.Source, vat.CheckedOn));
                checks.Add(Summary(vat, CheckOutcome.Problem, "brak podmiotu w wykazie podatników VAT"));
                return;
        }

        var subject = vat.Value!;

        switch (subject.Status)
        {
            case VatStatus.Active:
                checks.Add(Summary(vat, CheckOutcome.Passed, "czynny podatnik VAT"));
                break;

            case VatStatus.Exempt:
                findings.Add(new Finding(
                    FindingCodes.VatExempt,
                    RiskLevel.Low,
                    "Firma jest podatnikiem zwolnionym z VAT.",
                    "Faktury od tego kontrahenta nie będą zawierały VAT – to normalne dla podatnika zwolnionego, nie jest to sygnał ostrzegawczy.",
                    vat.Source, vat.CheckedOn));
                checks.Add(Summary(vat, CheckOutcome.Info, "podatnik zwolniony z VAT"));
                break;

            case VatStatus.NotRegistered:
                var removal = subject.RemovalDate is { } date
                    ? $" Wykreślono go z rejestru VAT {Format(date)}{(subject.RemovalBasis is not null ? $" (podstawa: {subject.RemovalBasis})" : "")}."
                    : "";
                findings.Add(new Finding(
                    FindingCodes.VatNotActive,
                    RiskLevel.High,
                    $"Podmiot nie jest czynnym podatnikiem VAT.{removal}",
                    "Nie odliczaj VAT z faktury od tego kontrahenta, dopóki nie wyjaśnisz jego statusu. Poproś o wyjaśnienie i skonsultuj się z księgowością.",
                    vat.Source, vat.CheckedOn));
                checks.Add(Summary(vat, CheckOutcome.Problem, "podmiot niezarejestrowany jako podatnik VAT"));
                break;
        }
    }

    private static void ScoreAccount(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        if (profile.Account is not { } account)
        {
            checks.Add(new CheckSummary(SourceNames.WhiteList, profile.CheckedOn, CheckOutcome.Info,
                "nie podano rachunku z faktury – nie sprawdzono, czy jest na Białej liście"));
            return;
        }

        if (account.Status == CheckStatus.Unavailable)
        {
            checks.Add(NotChecked(account, "czy rachunek z faktury jest na Białej liście"));
            return;
        }

        var result = account.Value!;
        var number = result.Account.ToDisplayString();

        if (result.Assigned)
        {
            checks.Add(Summary(account, CheckOutcome.Passed, $"rachunek {number} jest na Białej liście tej firmy"));
            return;
        }

        findings.Add(new Finding(
            FindingCodes.AccountNotOnWhiteList,
            RiskLevel.High,
            $"Rachunek z faktury {number} nie jest przypisany do tej firmy na Białej liście VAT.",
            "Poproś kontrahenta o fakturę z rachunkiem widocznym na Białej liście. Przy płatności powyżej 15 000 zł na rachunek spoza listy " +
            "możesz stracić prawo do zaliczenia wydatku w koszty i odpowiadać solidarnie za VAT kontrahenta; jeśli zapłata już nastąpiła, " +
            "w ciągu 7 dni można złożyć zawiadomienie ZAW-NR do urzędu skarbowego.",
            account.Source, account.CheckedOn));
        checks.Add(Summary(account, CheckOutcome.Problem, $"rachunek {number} NIE jest na Białej liście tej firmy"));
    }

    private static void ScoreKrs(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        var krs = profile.Krs;

        switch (krs.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(NotChecked(krs, "zarząd, sprawozdania finansowe, likwidacja i upadłość"));
                return;

            case CheckStatus.NotApplicable:
                checks.Add(Summary(krs, CheckOutcome.Info, krs.Note ?? "podmiot spoza KRS – nie sprawdzono"));
                return;

            case CheckStatus.NotFound:
                findings.Add(new Finding(
                    FindingCodes.KrsNotFound,
                    RiskLevel.Medium,
                    "Numeru KRS podanego na Białej liście nie znaleziono w KRS.",
                    "Poproś kontrahenta o aktualny odpis z KRS i porównaj dane z fakturą.",
                    krs.Source, krs.CheckedOn));
                checks.Add(Summary(krs, CheckOutcome.Problem, "brak wpisu w KRS"));
                return;
        }

        var entry = krs.Value!;
        checks.Add(Summary(krs, CheckOutcome.Passed,
            $"wpis w KRS nr {entry.KrsNumber}{(entry.RegisteredOn.HasValue ? $", zarejestrowana {Format(entry.RegisteredOn.Value)}" : "")}"));

        if (entry.Bankruptcy is { } bankruptcy)
        {
            var details = bankruptcy.DeclaredOn is { } declared ? $" (ogłoszona {Format(declared)}{(bankruptcy.CaseNumber is not null ? $", sygn. {bankruptcy.CaseNumber}" : "")})" : "";
            findings.Add(new Finding(
                FindingCodes.Bankruptcy,
                RiskLevel.High,
                $"Wobec spółki ogłoszono upadłość{details}.",
                "Nie dokonuj przedpłat. Ustal z syndykiem, na jaki rachunek płacić, i oceń, czy kontrahent będzie w stanie zrealizować umowę.",
                krs.Source, krs.CheckedOn));
            checks.Add(Summary(krs, CheckOutcome.Problem, "ogłoszona upadłość (dział 6)"));
        }

        if (entry.InLiquidation)
        {
            findings.Add(new Finding(
                FindingCodes.Liquidation,
                RiskLevel.High,
                "Spółka jest w likwidacji, czyli kończy działalność.",
                "Unikaj długoterminowych umów i przedpłat. Sprawy bieżące uzgadniaj z likwidatorem.",
                krs.Source, krs.CheckedOn));
            checks.Add(Summary(krs, CheckOutcome.Problem, "spółka w likwidacji (dział 6)"));
        }

        if (entry.Bankruptcy is null && !entry.InLiquidation)
            checks.Add(Summary(krs, CheckOutcome.Passed, "brak wzmianek o likwidacji lub upadłości (dział 6)"));

        ScoreFinancialStatements(profile.CheckedOn, krs, entry, findings, checks);
    }

    private static void ScoreFinancialStatements(
        DateOnly today, SourceCheck<KrsEntry> krs, KrsEntry entry, List<Finding> findings, List<CheckSummary> checks)
    {
        if (entry.RegisteredOn is not { } registered)
            return;

        // Gdy działalność rozpoczęto w II połowie roku, pierwszy rok obrotowy można połączyć z następnym.
        var firstFiscalYear = registered.Month > 6 ? registered.Year + 1 : registered.Year;
        var latestRequired = LatestRequiredFiscalYear(today);
        var required = new[] { latestRequired - 1, latestRequired }.Where(y => y >= firstFiscalYear).ToList();

        if (required.Count == 0)
        {
            checks.Add(Summary(krs, CheckOutcome.Info, "spółka nie musiała jeszcze złożyć żadnego sprawozdania finansowego"));
            return;
        }

        var filed = entry.FinancialStatements.Select(f => f.FiscalYear).ToHashSet();
        var missing = required.Where(y => !filed.Contains(y)).ToList();

        if (missing.Count == 0)
        {
            checks.Add(Summary(krs, CheckOutcome.Passed, $"sprawozdania finansowe złożone za {string.Join(" i ", required)}"));
            return;
        }

        var missingText = string.Join(" i ", missing);
        findings.Add(new Finding(
            FindingCodes.FinancialStatementsMissing,
            RiskLevel.Medium,
            $"W KRS brakuje sprawozdania finansowego za {missingText}, choć termin złożenia już minął.",
            "Poproś kontrahenta o ostatnie sprawozdanie finansowe albo o wyjaśnienie, dlaczego nie zostało złożone do KRS.",
            krs.Source, krs.CheckedOn));
        checks.Add(Summary(krs, CheckOutcome.Problem, $"brak sprawozdania finansowego za {missingText}"));
    }

    private static void ScoreCompanyAge(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        // Data wpisu do KRS, a dla firm spoza KRS – data rejestracji z Białej listy.
        DateOnly date;
        string source;

        if (profile.Krs.Value?.RegisteredOn is { } krsDate)
            (date, source) = (krsDate, SourceNames.Krs);
        else if (profile.Vat.Value?.RegistrationLegalDate is { } vatDate)
            (date, source) = (vatDate, SourceNames.WhiteList);
        else
            return;

        if (profile.CheckedOn >= date.AddMonths(12))
            return;

        var months = (profile.CheckedOn.Year - date.Year) * 12 + profile.CheckedOn.Month - date.Month;
        findings.Add(new Finding(
            FindingCodes.YoungCompany,
            RiskLevel.Low,
            $"Firma działa krócej niż rok (zarejestrowana {Format(date)}, ok. {months} mies. temu).",
            "Młoda firma nie musi być ryzykowna, ale przy pierwszej współpracy rozważ mniejsze zamówienie lub płatność po dostawie.",
            source, profile.CheckedOn));
        checks.Add(new CheckSummary(source, profile.CheckedOn, CheckOutcome.Info, $"firma działa od {Format(date)} (krócej niż rok)"));
    }

    private static void ScoreBeneficialOwners(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        var crbr = profile.BeneficialOwners;

        switch (crbr.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(NotChecked(crbr, "beneficjenci rzeczywiści"));
                return;

            case CheckStatus.NotApplicable:
                return;

            case CheckStatus.NotFound:
                findings.Add(new Finding(
                    FindingCodes.BeneficialOwnersMissing,
                    RiskLevel.Medium,
                    "Spółka nie zgłosiła beneficjentów rzeczywistych do CRBR, choć ma taki obowiązek.",
                    "Poproś kontrahenta o informację, kto jest beneficjentem rzeczywistym, albo sprawdź CRBR ręcznie: https://crbr.podatki.gov.pl.",
                    crbr.Source, crbr.CheckedOn));
                checks.Add(Summary(crbr, CheckOutcome.Problem, "brak zgłoszenia w CRBR"));
                return;
        }

        var owners = crbr.Value!.Owners;
        checks.Add(Summary(crbr, CheckOutcome.Passed,
            $"zgłoszeni beneficjenci rzeczywiści: {string.Join(", ", owners.Select(o => o.FullName))}"));
    }

    private static void ScoreSanctions(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        var sanctions = profile.Sanctions;

        switch (sanctions.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(NotChecked(sanctions, "listy sankcyjne"));
                return;

            case CheckStatus.NotApplicable or CheckStatus.NotFound:
                return;
        }

        var result = sanctions.Value!;

        if (result.Hits.Count == 0)
        {
            checks.Add(Summary(sanctions, CheckOutcome.Passed,
                $"brak trafień na liście sankcyjnej (sprawdzono: {string.Join(", ", result.CheckedNames)})"));
            return;
        }

        var hits = string.Join("; ", result.Hits.Select(h =>
            $"{h.QueriedName} → „{h.ListedName}”{(h.Programme is not null ? $" (program sankcyjny: {h.Programme})" : "")}"));
        findings.Add(new Finding(
            FindingCodes.SanctionsHit,
            RiskLevel.Critical,
            $"Trafienie na liście sankcyjnej: {hits}.",
            "Wstrzymaj płatność i współpracę do czasu wyjaśnienia. Skonsultuj sprawę z działem compliance lub prawnikiem – " +
            "udostępnianie środków podmiotom z listy sankcyjnej jest zakazane.",
            sanctions.Source, sanctions.CheckedOn));
        checks.Add(Summary(sanctions, CheckOutcome.Problem, $"trafienie na liście sankcyjnej: {hits}"));
    }

    private static void ScoreRelations(CompanyProfile profile, List<Finding> findings, List<CheckSummary> checks)
    {
        var relations = profile.Relations;

        switch (relations.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(NotChecked(relations, "powiązania osobowe z innymi firmami"));
                return;

            case CheckStatus.NotApplicable:
                if (relations.Note is { } note)
                    checks.Add(Summary(relations, CheckOutcome.Info, note));
                return;

            case CheckStatus.NotFound:
                return;
        }

        var companies = relations.Value!.Companies;
        var risky = companies.Where(c => c.Verdict >= RiskLevel.High).ToList();

        if (risky.Count == 0)
        {
            checks.Add(Summary(relations, CheckOutcome.Passed, companies.Count == 0
                ? "członkowie zarządu nie zasiadają w zarządach innych firm z rejestru"
                : $"powiązane firmy bez wysokiego ryzyka: {string.Join(", ", companies.Select(c => c.Link.CompanyName))}"));
            return;
        }

        var description = string.Join("; ", risky.Select(c =>
            $"{c.Link.PersonName} zasiada też w zarządzie firmy {c.Link.CompanyName} (NIP {c.Link.Nip.ToDisplayString()}, funkcja: {c.Link.Role.ToLowerInvariant()}), " +
            $"której wstępna ocena ryzyka to {c.Verdict!.Value.ToPolishVerdict()}"));
        findings.Add(new Finding(
            FindingCodes.LinkedHighRisk,
            RiskLevel.Medium,
            $"Powiązanie osobowe z firmą o wysokim ryzyku: {description}.",
            "Sprawdź powiązaną firmę (możesz zapytać o nią po NIP-ie) i oceń, czy to powiązanie wpływa na wiarygodność kontrahenta.",
            relations.Source, relations.CheckedOn));
        checks.Add(Summary(relations, CheckOutcome.Problem, $"powiązanie osobowe z firmą o wysokim ryzyku: {string.Join(", ", risky.Select(c => c.Link.CompanyName))}"));
    }

    private static CheckSummary Summary<T>(SourceCheck<T> check, CheckOutcome outcome, string summary) where T : class =>
        new(check.Source, check.CheckedOn, outcome, summary);

    private static CheckSummary NotChecked<T>(SourceCheck<T> check, string what) where T : class =>
        new(check.Source, check.CheckedOn, CheckOutcome.NotChecked,
            $"NIE SPRAWDZONO ({what}) – źródło nie odpowiedziało{(check.Note is not null ? $": {check.Note}" : "")}; tego punktu nie oceniono");

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
