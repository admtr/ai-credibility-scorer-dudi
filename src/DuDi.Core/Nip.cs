using System.Text;

namespace DuDi.Core;

/// <summary>
/// NIP – 10-cyfrowy numer identyfikacji podatkowej. Instancja istnieje tylko dla numeru z poprawną sumą kontrolną,
/// więc do źródeł danych nigdy nie trafia „wymyślony” lub przekręcony NIP.
/// </summary>
public readonly record struct Nip
{
    private static readonly int[] Weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

    private Nip(string value) => Value = value;

    /// <summary>Same cyfry, bez myślników i prefiksu PL.</summary>
    public string Value { get; }

    public static bool TryParse(string? input, out Nip nip) => TryParse(input, out nip, out _);

    /// <summary>Akceptuje zapisy: 1234567890, 123-456-78-90, 123-45-67-890, PL1234567890.</summary>
    public static bool TryParse(string? input, out Nip nip, out string? error)
    {
        nip = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Nie podano NIP-u.";
            return false;
        }

        var digits = Normalize(input);

        if (digits is null)
        {
            error = $"„{input.Trim()}” nie wygląda na NIP – NIP ma 10 cyfr (myślniki i spacje są dozwolone).";
            return false;
        }

        if (!HasValidChecksum(digits))
        {
            error = $"NIP {digits} ma niepoprawną sumę kontrolną – prawdopodobnie jest w nim literówka.";
            return false;
        }

        nip = new Nip(digits);
        error = null;
        return true;
    }

    public static Nip Parse(string input) =>
        TryParse(input, out var nip, out var error) ? nip : throw new FormatException(error);

    /// <summary>Zapis 123-456-78-90, czytelny dla użytkownika.</summary>
    public string ToDisplayString() => $"{Value[..3]}-{Value[3..6]}-{Value[6..8]}-{Value[8..]}";

    public override string ToString() => Value ?? string.Empty;

    private static string? Normalize(string input)
    {
        var text = input.Trim();

        if (text.StartsWith("PL", StringComparison.OrdinalIgnoreCase))
            text = text[2..];

        var digits = new StringBuilder(10);

        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
                digits.Append(c);
            else if (c is not ('-' or ' '))
                return null;
        }

        return digits.Length == 10 ? digits.ToString() : null;
    }

    private static bool HasValidChecksum(string digits)
    {
        var sum = 0;

        for (var i = 0; i < Weights.Length; i++)
            sum += (digits[i] - '0') * Weights[i];

        var checkDigit = sum % 11;

        // Reszta 10 oznacza, że taki numer nie mógł zostać nadany.
        return checkDigit != 10 && checkDigit == digits[9] - '0';
    }
}
