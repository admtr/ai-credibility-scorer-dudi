using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena powiązań osobowych członków zarządu z innymi spółkami o wysokim ryzyku.</summary>
public sealed class RelationsRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        var relations = profile.Relations;

        switch (relations.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(relations.NotChecked("powiązania osobowe z innymi firmami"));
                return;

            case CheckStatus.NotApplicable:
                if (relations.Note is { } note)
                    checks.Add(relations.Summary(CheckOutcome.Info, note));
                return;

            case CheckStatus.NotFound:
                return;
        }

        var companies = relations.Value!.Companies;
        var risky = companies.Where(c => c.Verdict >= RiskLevel.High).ToList();

        if (risky.Count == 0)
        {
            checks.Add(relations.Summary(CheckOutcome.Passed, companies.Count == 0
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
        checks.Add(relations.Summary(CheckOutcome.Problem, $"powiązanie osobowe z firmą o wysokim ryzyku: {string.Join(", ", risky.Select(c => c.Link.CompanyName))}"));
    }
}
