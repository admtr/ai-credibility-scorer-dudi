using System.Globalization;
using System.Text.Json;

namespace DuDi.Sources;

internal static class RegistryJson
{
    /// <summary>Nazwy pól w API są w camelCase (Biała lista po angielsku, KRS po polsku), więc wystarczy dopasowanie bez wielkości liter.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static T Deserialize<T>(string json, string description) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidDataException($"Pusty dokument JSON: {description}.");

    /// <summary>Biała lista: „2026-11-03”.</summary>
    public static DateOnly? ParseIsoDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    /// <summary>KRS: „03.11.2026”.</summary>
    public static DateOnly? ParsePolishDate(string? value) =>
        DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
