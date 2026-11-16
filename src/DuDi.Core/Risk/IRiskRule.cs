using System.Globalization;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk;

/// <summary>Pojedyncza reguła oceny ryzyka kontrahenta.</summary>
public interface IRiskRule
{
    void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks);
}

/// <summary>Silnik wyliczający raport ryzyka na podstawie zestawu reguł.</summary>
public interface IRiskScorer
{
    RiskReport Score(CompanyProfile profile);
}

internal static class RiskRuleHelpers
{
    public static CheckSummary Summary<T>(this SourceCheck<T> check, CheckOutcome outcome, string summary) where T : class =>
        new(check.Source, check.CheckedOn, outcome, summary);

    public static CheckSummary NotChecked<T>(this SourceCheck<T> check, string what) where T : class =>
        new(check.Source, check.CheckedOn, CheckOutcome.NotChecked,
            $"NIE SPRAWDZONO ({what}) – źródło nie odpowiedziało{(check.Note is not null ? $": {check.Note}" : "")}; tego punktu nie oceniono");

    public static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
