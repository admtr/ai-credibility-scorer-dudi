using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena zgłoszenia beneficjentów rzeczywistych w CRBR.</summary>
public sealed class BeneficialOwnersRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        var crbr = profile.BeneficialOwners;

        switch (crbr.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(crbr.NotChecked("beneficjenci rzeczywiści"));
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
                checks.Add(crbr.Summary(CheckOutcome.Problem, "brak zgłoszenia w CRBR"));
                return;
        }

        var owners = crbr.Value!.Owners;
        checks.Add(crbr.Summary(CheckOutcome.Passed,
            $"zgłoszeni beneficjenci rzeczywiści: {string.Join(", ", owners.Select(o => o.FullName))}"));
    }
}
