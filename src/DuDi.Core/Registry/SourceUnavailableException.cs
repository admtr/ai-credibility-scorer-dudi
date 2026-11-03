namespace DuDi.Core.Registry;

/// <summary>Rejestr nie odpowiedział (timeout, błąd serwera, limit zapytań, symulowana awaria w trybie Mock).</summary>
public sealed class SourceUnavailableException(string sourceName, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>Nazwa rejestru, np. <see cref="SourceNames.WhiteList"/>.</summary>
    public string SourceName { get; } = sourceName;
}
