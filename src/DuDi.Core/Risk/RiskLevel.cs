namespace DuDi.Core.Risk;

/// <summary>Waga pojedynczego ustalenia i zarazem werdykt ogólny (najwyższa waga wśród ustaleń).</summary>
public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public static class RiskLevelExtensions
{
    /// <summary>Werdykt: „NISKIE ryzyko”.</summary>
    public static string ToPolishVerdict(this RiskLevel level) => level switch
    {
        RiskLevel.Low => "NISKIE",
        RiskLevel.Medium => "ŚREDNIE",
        RiskLevel.High => "WYSOKIE",
        RiskLevel.Critical => "KRYTYCZNE",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    /// <summary>Waga ustalenia: „waga WYSOKA”.</summary>
    public static string ToPolishWeight(this RiskLevel level) => level switch
    {
        RiskLevel.Low => "NISKA",
        RiskLevel.Medium => "ŚREDNIA",
        RiskLevel.High => "WYSOKA",
        RiskLevel.Critical => "KRYTYCZNA",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}
