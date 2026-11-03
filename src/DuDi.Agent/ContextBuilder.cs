using System.Globalization;
using System.Text;
using DuDi.Core.Registry;
using DuDi.Core.Risk;
using DuDi.Core.Verification;

namespace DuDi.Agent;

/// <summary>
/// Zamienia <see cref="RiskReport"/> na zwięzły, stały w strukturze blok tekstu dla modelu.
/// Do modelu trafiają wyłącznie fakty z pochodzeniem (źródło + data), wyliczony werdykt i gotowe zalecenia – nie surowy JSON.
/// </summary>
/// <param name="isTestData">Tryb Mock: blok jest oznaczony jako dane testowe.</param>
public sealed class ContextBuilder(bool isTestData)
{
    public const string TestDataMarker = "[DANE TESTOWE]";

    public bool IsTestData => isTestData;

    public string Build(RiskReport report)
    {
        var p = report.Profile;
        var sb = new StringBuilder();

        sb.AppendLine("<dane_kontrahenta>");

        if (isTestData)
            sb.AppendLine($"{TestDataMarker} Dane z symulowanej bazy – fikcyjne firmy, nie prawdziwe rejestry.");

        sb.AppendLine($"NIP: {p.Nip.ToDisplayString()}");
        sb.AppendLine($"Data sprawdzenia: {Format(p.CheckedOn)}");

        if (!report.CompanyFound)
        {
            sb.AppendLine(p.Vat.Status == CheckStatus.Unavailable
                ? "WYNIK: Nie udało się sprawdzić firmy – Biała lista VAT nie odpowiedziała. Brak danych do oceny; zaproponuj ponowienie później."
                : "WYNIK: Nie znaleziono podmiotu o tym NIP w sprawdzonych rejestrach. Brak danych do oceny. " +
                  "Nie podawaj żadnych informacji o tej firmie. Brak na Białej liście nie musi oznaczać, że firma nie istnieje " +
                  "(np. działalność zwolniona z VAT), ale tego nie da się tu potwierdzić.");
            sb.AppendLine("</dane_kontrahenta>");
            return sb.ToString();
        }

        sb.AppendLine($"Nazwa: {p.Name}");
        sb.AppendLine($"WERDYKT (wyliczony regułami programu – nie zmieniaj go): ryzyko {report.Verdict!.Value.ToPolishVerdict()}");

        if (report.IsIncomplete)
            sb.AppendLine("UWAGA: część źródeł nie odpowiedziała – werdykt opiera się na niepełnych danych (patrz NIE SPRAWDZONO).");

        sb.AppendLine();
        sb.AppendLine("SPRAWDZONO:");

        foreach (var check in report.Checks)
            sb.AppendLine($"- [{Label(check.Outcome)}] {check.Source}, {Format(check.CheckedOn)}: {check.Summary}");

        sb.AppendLine();

        if (report.Findings.Count == 0)
        {
            sb.AppendLine("USTALENIA: brak – żadna reguła nie wykazała ryzyka.");
        }
        else
        {
            sb.AppendLine("USTALENIA (od najważniejszego):");

            foreach (var finding in report.Findings)
            {
                sb.AppendLine($"- waga {finding.Severity.ToPolishWeight()}: {finding.Description} ({finding.Source}, {Format(finding.CheckedOn)})");
                sb.AppendLine($"  Zalecenie: {finding.Advice}");
            }
        }

        AppendDetails(sb, p);
        sb.AppendLine("</dane_kontrahenta>");

        return sb.ToString();
    }

    private static void AppendDetails(StringBuilder sb, CompanyProfile p)
    {
        sb.AppendLine();
        sb.AppendLine("DANE SZCZEGÓŁOWE:");

        if (p.Vat.Value is { } vat)
        {
            sb.AppendLine($"- Adres: {vat.Address ?? "brak"}");
            sb.AppendLine($"- REGON: {vat.Regon ?? "brak"}");
            sb.AppendLine(vat.AccountNumbers.Count == 0
                ? "- Rachunki na Białej liście: brak"
                : $"- Rachunki na Białej liście: {string.Join(", ", vat.AccountNumbers.Select(a => a.ToDisplayString()))}");
        }

        if (p.Krs.Value is { } krs)
        {
            sb.AppendLine($"- Numer KRS: {krs.KrsNumber}, forma prawna: {krs.LegalForm?.ToLowerInvariant() ?? "brak"}");
            sb.AppendLine(krs.Board.Count == 0
                ? "- Zarząd: brak danych"
                : $"- Zarząd: {string.Join("; ", krs.Board.Select(m => $"{m.FullName} – {m.Function.ToLowerInvariant()}"))}");
            sb.AppendLine(krs.FinancialStatements.Count == 0
                ? "- Sprawozdania finansowe w KRS: brak"
                : $"- Sprawozdania finansowe w KRS za lata: {string.Join(", ", krs.FinancialStatements.Select(f => f.FiscalYear).Order())}");
        }

        if (p.BeneficialOwners.Value is { } owners)
            sb.AppendLine($"- Beneficjenci rzeczywiści (CRBR): {string.Join("; ", owners.Owners.Select(DescribeOwner))}");

        if (p.Relations.Value is { Companies.Count: > 0 } relations)
            sb.AppendLine($"- Powiązania osobowe: {string.Join("; ", relations.Companies.Select(DescribeRelation))}");
    }

    private static string DescribeOwner(BeneficialOwner owner)
    {
        var text = owner.FullName;

        if (owner.Ownership is not null)
            text += $" – {owner.Ownership}";

        if (owner.Citizenship is not null)
            text += $", obywatelstwo {owner.Citizenship.ToLowerInvariant()}";

        return text;
    }

    private static string DescribeRelation(RelatedCompany company)
    {
        var verdict = company.Verdict is { } level ? level.ToPolishVerdict() : "brak oceny";
        return $"{company.Link.PersonName} zasiada też w zarządzie: {company.Link.CompanyName}, " +
               $"NIP {company.Link.Nip.ToDisplayString()} (wstępne ryzyko: {verdict})";
    }

    private static string Label(CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.Passed => "OK",
        CheckOutcome.Problem => "PROBLEM",
        CheckOutcome.Info => "INFORMACJA",
        CheckOutcome.NotChecked => "NIE SPRAWDZONO",
        _ => outcome.ToString()
    };

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
