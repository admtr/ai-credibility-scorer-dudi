using System.Text;

namespace DuDi.Core;

/// <summary>
/// Polski numer rachunku bankowego (NRB, 26 cyfr) z poprawną sumą kontrolną IBAN.
/// W tej postaci (bez prefiksu PL) rachunki występują na Białej liście VAT.
/// </summary>
public readonly record struct BankAccountNumber
{
    private BankAccountNumber(string value) => Value = value;

    /// <summary>26 cyfr, bez spacji i prefiksu PL.</summary>
    public string Value { get; }

    public static bool TryParse(string? input, out BankAccountNumber account) => TryParse(input, out account, out _);

    /// <summary>Akceptuje zapisy: 26 cyfr, ze spacjami lub myślnikami, z prefiksem PL lub bez.</summary>
    public static bool TryParse(string? input, out BankAccountNumber account, out string? error)
    {
        account = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Nie podano numeru rachunku.";
            return false;
        }

        var digits = Normalize(input);

        if (digits is null)
        {
            error = $"„{input.Trim()}” nie wygląda na polski numer rachunku – powinien mieć 26 cyfr.";
            return false;
        }

        if (!HasValidChecksum(digits))
        {
            error = $"Numer rachunku {digits} ma niepoprawną sumę kontrolną – prawdopodobnie jest w nim literówka.";
            return false;
        }

        account = new BankAccountNumber(digits);
        error = null;
        return true;
    }

    public static BankAccountNumber Parse(string input) =>
        TryParse(input, out var account, out var error) ? account : throw new FormatException(error);

    /// <summary>Zapis „61 1090 1014 0000 0712 1981 2874”.</summary>
    public string ToDisplayString()
    {
        var groups = Enumerable.Range(0, 6).Select(i => Value.Substring(2 + i * 4, 4));
        return $"{Value[..2]} {string.Join(' ', groups)}";
    }

    public override string ToString() => Value ?? string.Empty;

    private static string? Normalize(string input)
    {
        var text = input.Trim();

        if (text.StartsWith("PL", StringComparison.OrdinalIgnoreCase))
            text = text[2..];

        var digits = new StringBuilder(26);

        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
                digits.Append(c);
            else if (c is not ('-' or ' '))
                return null;
        }

        return digits.Length == 26 ? digits.ToString() : null;
    }

    // Algorytm IBAN (ISO 13616): cyfry kontrolne na koniec, kraj "PL" jako 2521, wynik mod 97 musi być równy 1.
    private static bool HasValidChecksum(string digits)
    {
        var rearranged = digits[2..] + "2521" + digits[..2];
        var remainder = 0;

        foreach (var c in rearranged)
            remainder = (remainder * 10 + (c - '0')) % 97;

        return remainder == 1;
    }
}
