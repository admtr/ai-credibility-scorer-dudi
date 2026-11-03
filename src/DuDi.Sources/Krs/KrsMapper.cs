using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DuDi.Core;
using DuDi.Core.Registry;

namespace DuDi.Sources.Krs;

/// <summary>Odpis aktualny KRS → zwięzły <see cref="KrsEntry"/>. Do modelu nie trafia surowy, wielostronicowy JSON.</summary>
public static partial class KrsMapper
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static KrsEntry ToKrsEntry(KrsOdpisResponse response)
    {
        var odpis = response.Odpis ?? throw new InvalidDataException("Odpowiedź KRS nie zawiera sekcji „odpis”.");
        var header = odpis.NaglowekA ?? throw new InvalidDataException("Odpis KRS nie zawiera nagłówka.");
        var dane = odpis.Dane;
        var podmiot = dane?.Dzial1?.DanePodmiotu;
        var name = podmiot?.Nazwa ?? "(brak nazwy)";

        Nip? nip = Nip.TryParse(podmiot?.Identyfikatory?.Nip, out var parsed) ? parsed : null;

        var board = (dane?.Dzial2?.Reprezentacja?.Sklad ?? [])
            .Where(o => o.CzyZawieszona != true)
            .Select(o => new BoardMember(FullName(o), ToSentenceCase(o.FunkcjaWOrganie ?? "członek organu")))
            .ToList();

        var statements = (dane?.Dzial3?.WzmiankiOZlozonychDokumentach?.WzmiankaOZlozeniuRocznegoSprawozdaniaFinansowego ?? [])
            .Select(ToFiling)
            .OfType<FinancialStatementFiling>()
            .ToList();

        var bankruptcy = (dane?.Dzial6?.PostepowanieUpadlosciowe ?? [])
            .Where(p => !IsNonEmpty(p.OpisZakonczeniaProcesuUpadlosci))
            .Select(p => p.InformacjaOOgloszeniuUpadlosci)
            .OfType<KrsOrzeczenie>()
            .Where(o => o.Data is not null || o.Sygnatura is not null)
            .Select(o => new BankruptcyInfo(RegistryJson.ParsePolishDate(o.Data), o.OrganWydajacy, o.Sygnatura))
            .FirstOrDefault();

        if (bankruptcy is null && name.Contains("W UPADŁOŚCI", StringComparison.OrdinalIgnoreCase))
            bankruptcy = new BankruptcyInfo(null, null, null);

        var inLiquidation = IsNonEmpty(dane?.Dzial6?.Likwidacja)
            || name.Contains("W LIKWIDACJI", StringComparison.OrdinalIgnoreCase);

        return new KrsEntry(
            header.NumerKRS ?? throw new InvalidDataException("Odpis KRS nie zawiera numeru KRS."),
            name,
            podmiot?.FormaPrawna,
            nip,
            RegistryJson.ParsePolishDate(header.DataRejestracjiWKRS),
            board,
            statements,
            bankruptcy,
            inLiquidation);
    }

    private static string FullName(KrsOsoba osoba)
    {
        var parts = new[]
        {
            osoba.Imiona?.Imie,
            osoba.Imiona?.ImieDrugie,
            osoba.Nazwisko?.NazwiskoICzlon,
            osoba.Nazwisko?.NazwiskoCzlonDrugi
        };

        var fullName = string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));

        // KRS zapisuje dane wielkimi literami; „JAN KOWALSKI” → „Jan Kowalski”. Zamaskowane („F*****”) zostawiamy bez zmian.
        return fullName.Contains('*') ? fullName : Polish.TextInfo.ToTitleCase(fullName.ToLower(Polish));
    }

    private static FinancialStatementFiling? ToFiling(KrsWzmianka wzmianka)
    {
        var match = PeriodEndRegex().Match(wzmianka.ZaOkresOdDo ?? "");

        if (!match.Success || RegistryJson.ParsePolishDate(wzmianka.DataZlozenia) is not { } filedOn)
            return null;

        return new FinancialStatementFiling(int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture), filedOn);
    }

    private static string ToSentenceCase(string text)
    {
        var lower = text.ToLower(Polish);
        return lower.Length == 0 ? lower : char.ToUpper(lower[0], Polish) + lower[1..];
    }

    private static bool IsNonEmpty(JsonElement? element) => element is { } value && value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().Any(),
        JsonValueKind.Array => value.GetArrayLength() > 0,
        JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Null or JsonValueKind.Undefined or JsonValueKind.False => false,
        _ => true
    };

    // Rok obrotowy = rok daty końcowej okresu: „OD 01.01.2025 DO 31.12.2025” → 2025.
    [GeneratedRegex(@"DO\s+\d{2}\.\d{2}\.(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PeriodEndRegex();
}
