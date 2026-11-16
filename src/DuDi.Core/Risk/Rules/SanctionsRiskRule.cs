using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena trafień na listach sankcyjnych (firma, członkowie zarządu, beneficjenci rzeczywiści).</summary>
public sealed class SanctionsRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        var sanctions = profile.Sanctions;

        switch (sanctions.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(sanctions.NotChecked("listy sankcyjne"));
                return;

            case CheckStatus.NotApplicable or CheckStatus.NotFound:
                return;
        }

        var result = sanctions.Value!;

        if (result.Hits.Count == 0)
        {
            checks.Add(sanctions.Summary(CheckOutcome.Passed,
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
        checks.Add(sanctions.Summary(CheckOutcome.Problem, $"trafienie na liście sankcyjnej: {hits}"));
    }
}
