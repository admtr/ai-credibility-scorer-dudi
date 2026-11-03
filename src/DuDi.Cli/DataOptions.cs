using DuDi.Sources.Mock;

namespace DuDi.Cli;

public enum DataSourceKind
{
    /// <summary>Symulowana baza z plików (data/fixtures).</summary>
    Mock,

    /// <summary>Prawdziwe API rejestrów – etap 3–4.</summary>
    Live
}

/// <summary>Sekcja <c>Data</c> w appsettings.json.</summary>
public sealed class DataOptions
{
    public const string SectionName = "Data";

    public DataSourceKind Source { get; set; } = DataSourceKind.Mock;

    public MockDataOptions Mock { get; set; } = new();
}
