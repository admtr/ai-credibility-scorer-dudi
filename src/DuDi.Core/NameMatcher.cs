using System.Globalization;
using System.Text;

namespace DuDi.Core;

/// <summary>
/// Porównywanie nazw osób i firm między rejestrami (np. „Kowalski Jan” z listy sankcyjnej i „JAN KOWALSKI” z KRS).
/// </summary>
/// <remarks>
/// Świadomie prosta wersja: bez diakrytyków, bez wielkości liter i kolejności słów.
/// Nie obsługuje odmiany przez przypadki ani różnych transliteracji (np. Siemionow / Semenov) – to ograniczenie
/// opisane w documents/architecture.md, do rozwinięcia przy prawdziwej liście sankcyjnej.
/// </remarks>
public static class NameMatcher
{
    /// <summary>
    /// Nazwy pasują, gdy mają ten sam zestaw słów albo gdy krótsza (co najmniej 2 słowa) zawiera się w dłuższej –
    /// wtedy „Jan Kowalski” pasuje do „Jan Maria Kowalski”.
    /// </summary>
    public static bool Matches(string? first, string? second)
    {
        var a = Tokenize(first);
        var b = Tokenize(second);

        if (a.Count == 0 || b.Count == 0)
            return false;

        if (a.SetEquals(b))
            return true;

        var (shorter, longer) = a.Count <= b.Count ? (a, b) : (b, a);
        return shorter.Count >= 2 && shorter.IsSubsetOf(longer);
    }

    public static string Normalize(string? name) => string.Join(' ', Tokenize(name).Order(StringComparer.Ordinal));

    private static HashSet<string> Tokenize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var plain = RemoveDiacritics(name).ToUpperInvariant();
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var current = new StringBuilder();

        foreach (var c in plain)
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    private static string RemoveDiacritics(string text)
    {
        // „ł” nie rozkłada się w Unicode na literę i znak diakrytyczny, więc zamieniamy je ręcznie.
        var decomposed = text.Replace('ł', 'l').Replace('Ł', 'L').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c);
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }
}
