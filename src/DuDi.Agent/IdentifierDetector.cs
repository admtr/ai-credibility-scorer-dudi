using System.Text.RegularExpressions;
using DuDi.Core;

namespace DuDi.Agent;

/// <param name="InvalidNips">Ciągi wyglądające jak NIP, ale z błędną sumą kontrolną – agent powinien o nich powiedzieć.</param>
public sealed record DetectedIdentifiers(
    IReadOnlyList<Nip> Nips,
    IReadOnlyList<BankAccountNumber> Accounts,
    IReadOnlyList<string> InvalidNips);

/// <summary>Wyszukuje NIP-y i numery rachunków w wiadomości użytkownika (regex + suma kontrolna).</summary>
public static partial class IdentifierDetector
{
    public static DetectedIdentifiers Detect(string text)
    {
        var accounts = new List<BankAccountNumber>();

        // Najpierw rachunki: 26 cyfr rachunku zawiera ciągi, które mogłyby wyglądać jak NIP.
        var withoutAccounts = AccountRegex().Replace(text, match =>
        {
            if (BankAccountNumber.TryParse(match.Value, out var account) && !accounts.Contains(account))
                accounts.Add(account);

            return " ";
        });

        var nips = new List<Nip>();
        var invalid = new List<string>();

        foreach (Match match in NipRegex().Matches(withoutAccounts))
        {
            if (Nip.TryParse(match.Value, out var nip))
            {
                if (!nips.Contains(nip))
                    nips.Add(nip);
            }
            else
            {
                invalid.Add(match.Value.Trim());
            }
        }

        return new DetectedIdentifiers(nips, accounts, invalid);
    }

    // 26 cyfr, opcjonalnie z prefiksem PL i spacjami/myślnikami w grupach po 4: „PL61 1090 1014 0000 0712 1981 2874”.
    [GeneratedRegex(@"(?<!\d)(?:PL\s?)?\d{2}(?:[ -]?\d{4}){6}(?!\d)", RegexOptions.IgnoreCase)]
    private static partial Regex AccountRegex();

    // 10 cyfr: 1234567890, 123-456-78-90, 123-45-67-890, z opcjonalnym prefiksem PL.
    [GeneratedRegex(@"(?<!\d)(?:PL\s?)?(?:\d{3}[- ]?\d{3}[- ]?\d{2}[- ]?\d{2}|\d{3}[- ]\d{2}[- ]\d{2}[- ]\d{3})(?!\d)", RegexOptions.IgnoreCase)]
    private static partial Regex NipRegex();
}
