using DuDi.Core.Registry;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena stażu działalności firmy (poniżej 12 miesięcy od rejestracji).</summary>
public sealed class CompanyAgeRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
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
            $"Firma działa krócej niż rok (zarejestrowana {RiskRuleHelpers.Format(date)}, ok. {months} mies. temu).",
            "Młoda firma nie musi być ryzykowna, ale przy pierwszej współpracy rozważ mniejsze zamówienie lub płatność po dostawie.",
            source, profile.CheckedOn));
        checks.Add(new CheckSummary(source, profile.CheckedOn, CheckOutcome.Info, $"firma działa od {RiskRuleHelpers.Format(date)} (krócej niż rok)"));
    }
}
