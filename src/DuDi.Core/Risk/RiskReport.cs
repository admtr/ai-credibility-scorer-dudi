using DuDi.Core.Verification;

namespace DuDi.Core.Risk;

/// <summary>Jedno ustalenie reguły oceny ryzyka.</summary>
/// <param name="Code">Stały kod reguły (<see cref="FindingCodes"/>), wygodny w testach i ewaluacji.</param>
/// <param name="Advice">Zalecany krok dla użytkownika – agent korzysta z niego zamiast wymyślać własne zalecenia.</param>
public sealed record Finding(
    string Code,
    RiskLevel Severity,
    string Description,
    string Advice,
    string Source,
    DateOnly CheckedOn);

public enum CheckOutcome
{
    /// <summary>Sprawdzone, bez zastrzeżeń.</summary>
    Passed,

    /// <summary>Sprawdzone, jest problem (powiązany z ustaleniem).</summary>
    Problem,

    /// <summary>Informacja bez wpływu na ryzyko albo punkt, który nie dotyczy tej firmy.</summary>
    Info,

    /// <summary>Źródło nie odpowiedziało – punkt nie został oceniony.</summary>
    NotChecked
}

/// <summary>Pozycja listy „Co sprawdziliśmy”.</summary>
public sealed record CheckSummary(string Source, DateOnly CheckedOn, CheckOutcome Outcome, string Summary);

/// <summary>Wynik oceny kontrahenta. Liczony deterministycznie przez <see cref="RiskScorer"/>, LLM tylko go tłumaczy.</summary>
public sealed record RiskReport(
    CompanyProfile Profile,
    RiskLevel? Verdict,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<CheckSummary> Checks)
{
    /// <summary>Czy firma została znaleziona w którymkolwiek rejestrze. Gdy nie – <see cref="Verdict"/> jest <c>null</c>.</summary>
    public bool CompanyFound => Profile.IsKnown;

    /// <summary>Czy któreś ze źródeł nie odpowiedziało – wtedy werdykt opiera się na niepełnych danych.</summary>
    public bool IsIncomplete => Checks.Any(c => c.Outcome == CheckOutcome.NotChecked);
}
