namespace DuDi.Sources.Mock;

public sealed class MockDataOptions
{
    /// <summary>Katalog symulowanej bazy; ścieżka względna liczona od katalogu aplikacji.</summary>
    public string FixturesPath { get; set; } = "data/fixtures";

    /// <summary>
    /// Źródła, których awarię symulujemy (WhiteList, Krs, Crbr, Sanctions, Relations).
    /// Pozwala pokazać i przetestować zachowanie agenta, gdy rejestr nie odpowiada.
    /// </summary>
    public List<string> UnavailableSources { get; set; } = [];
}
