using DuDi.Core;
using DuDi.Core.Registry;
using DuDi.Sources.Crbr;
using DuDi.Sources.Krs;
using DuDi.Sources.Sanctions;
using DuDi.Sources.WhiteList;

namespace DuDi.Sources.Mock;

/// <summary>
/// Symulowana baza rejestrów. Pliki są zapisane w formacie odpowiedzi prawdziwych API i przechodzą przez te same mappery,
/// których użyje klient HTTP – przejście na internet to podmiana implementacji <see cref="IRegistrySource"/>.
/// </summary>
/// <remarks>
/// Układ katalogu <see cref="MockDataOptions.FixturesPath"/>:
/// <c>whitelist/{nip}.json</c> – odpowiedź /api/search/nip Białej listy,
/// <c>krs/{numerKRS}.json</c> – odpis aktualny KRS,
/// <c>crbr/{nip}.json</c> – zgłoszenie CRBR (format własny, CRBR nie ma publicznego API),
/// <c>sanctions/eu-sanctions.csv</c> – lista sankcyjna w formacie CSV UE.
/// Wszystkie firmy i osoby są fikcyjne.
/// </remarks>
public sealed class MockRegistrySource : IRegistrySource
{
    public const string WhiteListKey = "WhiteList";
    public const string KrsKey = "Krs";
    public const string CrbrKey = "Crbr";
    public const string SanctionsKey = "Sanctions";
    public const string RelationsKey = "Relations";

    private readonly Dictionary<Nip, VatSubject> vatSubjects;
    private readonly Dictionary<string, KrsEntry> krsEntries;
    private readonly Dictionary<Nip, BeneficialOwnersInfo> beneficialOwners;
    private readonly IReadOnlyList<SanctionsListEntry> sanctions;
    private readonly HashSet<string> unavailableSources;

    public MockRegistrySource(MockDataOptions options)
    {
        var root = ResolvePath(options.FixturesPath);

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Nie znaleziono symulowanej bazy w katalogu {root}.");

        vatSubjects = LoadJsonFiles<WhiteListSearchResponse>(Path.Combine(root, "whitelist"))
            .Select(WhiteListMapper.ToVatSubject)
            .OfType<VatSubject>()
            .ToDictionary(s => s.Nip);

        krsEntries = LoadJsonFiles<KrsOdpisResponse>(Path.Combine(root, "krs"))
            .Select(KrsMapper.ToKrsEntry)
            .ToDictionary(e => e.KrsNumber);

        beneficialOwners = LoadJsonFiles<CrbrEntry>(Path.Combine(root, "crbr"))
            .ToDictionary(e => Nip.Parse(e.Nip ?? ""), e => e.ToBeneficialOwnersInfo());

        var sanctionsFile = Path.Combine(root, "sanctions", "eu-sanctions.csv");
        sanctions = File.Exists(sanctionsFile) ? EuSanctionsCsv.Parse(File.ReadAllText(sanctionsFile)) : [];

        unavailableSources = new HashSet<string>(options.UnavailableSources, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Firmy w symulowanej bazie – do komendy /firmy i do przygotowania demo.</summary>
    public IReadOnlyList<(Nip Nip, string Name)> Companies =>
        vatSubjects.Values.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => (s.Nip, s.Name)).ToList();

    public IReadOnlyCollection<string> UnavailableSources => unavailableSources;

    public Task<VatSubject?> GetVatSubjectAsync(Nip nip, DateOnly date, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(WhiteListKey, SourceNames.WhiteList);
        return Task.FromResult<VatSubject?>(vatSubjects.GetValueOrDefault(nip));
    }

    public Task<bool> IsAccountAssignedAsync(Nip nip, BankAccountNumber account, DateOnly date, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(WhiteListKey, SourceNames.WhiteList);

        // Tak jak prawdziwy endpoint check: dla nieznanego NIP-u odpowiedź to „NIE”.
        var assigned = vatSubjects.TryGetValue(nip, out var subject) && subject.AccountNumbers.Contains(account);
        return Task.FromResult(assigned);
    }

    public Task<KrsEntry?> GetKrsEntryAsync(string krsNumber, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(KrsKey, SourceNames.Krs);
        return Task.FromResult<KrsEntry?>(krsEntries.GetValueOrDefault(krsNumber.Trim()));
    }

    public Task<BeneficialOwnersInfo?> GetBeneficialOwnersAsync(Nip nip, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(CrbrKey, SourceNames.Crbr);
        return Task.FromResult<BeneficialOwnersInfo?>(beneficialOwners.GetValueOrDefault(nip));
    }

    public Task<IReadOnlyList<SanctionHit>> CheckSanctionsAsync(string name, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(SanctionsKey, SourceNames.Sanctions);

        IReadOnlyList<SanctionHit> hits = sanctions
            .Where(e => NameMatcher.Matches(name, e.WholeName))
            .DistinctBy(e => e.LogicalId)
            .Select(e => new SanctionHit(name, e.WholeName, e.Programme, e.ReferenceNumber))
            .ToList();

        return Task.FromResult(hits);
    }

    public Task<IReadOnlyList<CompanyLink>> FindCompaniesByPersonAsync(string fullName, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable(RelationsKey, SourceNames.Relations);

        var links = new List<CompanyLink>();

        foreach (var entry in krsEntries.Values)
        {
            if (FindNip(entry) is not { } nip)
                continue;

            links.AddRange(entry.Board
                .Where(m => !m.IsMasked && NameMatcher.Matches(fullName, m.FullName))
                .Select(m => new CompanyLink(nip, entry.Name, m.FullName, m.Function)));
        }

        return Task.FromResult<IReadOnlyList<CompanyLink>>(links);
    }

    public static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private Nip? FindNip(KrsEntry entry) =>
        entry.Nip ?? vatSubjects.Values.FirstOrDefault(s => s.KrsNumber == entry.KrsNumber)?.Nip;

    private void ThrowIfUnavailable(string key, string sourceName)
    {
        if (unavailableSources.Contains(key))
            throw new SourceUnavailableException(sourceName, "symulowana awaria źródła (ustawienie Data:Mock:UnavailableSources)");
    }

    private static IEnumerable<T> LoadJsonFiles<T>(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
            yield return RegistryJson.Deserialize<T>(File.ReadAllText(file), Path.GetFileName(file));
    }
}
