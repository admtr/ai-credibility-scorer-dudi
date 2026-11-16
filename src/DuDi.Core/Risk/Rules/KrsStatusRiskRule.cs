using DuDi.Core.Registry;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena wpisu w KRS: upadłość, likwidacja oraz terminowość składania sprawozdań finansowych.</summary>
public sealed class KrsStatusRiskRule : IRiskRule
{
    /// <summary>
    /// Ostatni rok obrotowy, za który sprawozdanie powinno już być w KRS. Sprawozdanie za rok N zatwierdza się
    /// do 30 czerwca N+1 i składa w ciągu 15 dni, więc przyjmujemy termin 15 lipca. Zakładamy rok obrotowy = kalendarzowy.
    /// </summary>
    public static int LatestRequiredFiscalYear(DateOnly today)
    {
        var deadline = new DateOnly(today.Year, 7, 15);
        return today > deadline ? today.Year - 1 : today.Year - 2;
    }

    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        var krs = profile.Krs;

        switch (krs.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(krs.NotChecked("zarząd, sprawozdania finansowe, likwidacja i upadłość"));
                return;

            case CheckStatus.NotApplicable:
                checks.Add(krs.Summary(CheckOutcome.Info, krs.Note ?? "podmiot spoza KRS – nie sprawdzono"));
                return;

            case CheckStatus.NotFound:
                findings.Add(new Finding(
                    FindingCodes.KrsNotFound,
                    RiskLevel.Medium,
                    "Numeru KRS podanego na Białej liście nie znaleziono w KRS.",
                    "Poproś kontrahenta o aktualny odpis z KRS i porównaj dane z fakturą.",
                    krs.Source, krs.CheckedOn));
                checks.Add(krs.Summary(CheckOutcome.Problem, "brak wpisu w KRS"));
                return;
        }

        var entry = krs.Value!;
        checks.Add(krs.Summary(CheckOutcome.Passed,
            $"wpis w KRS nr {entry.KrsNumber}{(entry.RegisteredOn.HasValue ? $", zarejestrowana {RiskRuleHelpers.Format(entry.RegisteredOn.Value)}" : "")}"));

        if (entry.Bankruptcy is { } bankruptcy)
        {
            var details = bankruptcy.DeclaredOn is { } declared
                ? $" (ogłoszona {RiskRuleHelpers.Format(declared)}{(bankruptcy.CaseNumber is not null ? $", sygn. {bankruptcy.CaseNumber}" : "")})"
                : "";
            findings.Add(new Finding(
                FindingCodes.Bankruptcy,
                RiskLevel.High,
                $"Wobec spółki ogłoszono upadłość{details}.",
                "Nie dokonuj przedpłat. Ustal z syndykiem, na jaki rachunek płacić, i oceń, czy kontrahent będzie w stanie zrealizować umowę.",
                krs.Source, krs.CheckedOn));
            checks.Add(krs.Summary(CheckOutcome.Problem, "ogłoszona upadłość (dział 6)"));
        }

        if (entry.InLiquidation)
        {
            findings.Add(new Finding(
                FindingCodes.Liquidation,
                RiskLevel.High,
                "Spółka jest w likwidacji, czyli kończy działalność.",
                "Unikaj długoterminowych umów i przedpłat. Sprawy bieżące uzgadniaj z likwidatorem.",
                krs.Source, krs.CheckedOn));
            checks.Add(krs.Summary(CheckOutcome.Problem, "spółka w likwidacji (dział 6)"));
        }

        if (entry.Bankruptcy is null && !entry.InLiquidation)
            checks.Add(krs.Summary(CheckOutcome.Passed, "brak wzmianek o likwidacji lub upadłości (dział 6)"));

        EvaluateFinancialStatements(profile.CheckedOn, krs, entry, findings, checks);
    }

    private static void EvaluateFinancialStatements(
        DateOnly today, SourceCheck<KrsEntry> krs, KrsEntry entry, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        if (entry.RegisteredOn is not { } registered)
            return;

        // Gdy działalność rozpoczęto w II połowie roku, pierwszy rok obrotowy można połączyć z następnym.
        var firstFiscalYear = registered.Month > 6 ? registered.Year + 1 : registered.Year;
        var latestRequired = LatestRequiredFiscalYear(today);
        var required = new[] { latestRequired - 1, latestRequired }.Where(y => y >= firstFiscalYear).ToList();

        if (required.Count == 0)
        {
            checks.Add(krs.Summary(CheckOutcome.Info, "spółka nie musiała jeszcze złożyć żadnego sprawozdania finansowego"));
            return;
        }

        var filed = entry.FinancialStatements.Select(f => f.FiscalYear).ToHashSet();
        var missing = required.Where(y => !filed.Contains(y)).ToList();

        if (missing.Count == 0)
        {
            checks.Add(krs.Summary(CheckOutcome.Passed, $"sprawozdania finansowe złożone za {string.Join(" i ", required)}"));
            return;
        }

        var missingText = string.Join(" i ", missing);
        findings.Add(new Finding(
            FindingCodes.FinancialStatementsMissing,
            RiskLevel.Medium,
            $"W KRS brakuje sprawozdania finansowego za {missingText}, choć termin złożenia już minął.",
            "Poproś kontrahenta o ostatnie sprawozdanie finansowe albo o wyjaśnienie, dlaczego nie zostało złożone do KRS.",
            krs.Source, krs.CheckedOn));
        checks.Add(krs.Summary(CheckOutcome.Problem, $"brak sprawozdania finansowego za {missingText}"));
    }
}
