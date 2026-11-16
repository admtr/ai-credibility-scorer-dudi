using System.ComponentModel;
using DuDi.Core;
using DuDi.Core.Verification;
using Microsoft.Extensions.AI;

namespace DuDi.Agent.Tools;

/// <summary>Zapis wywołania narzędzia – do wyświetlenia w konsoli i do ewaluacji.</summary>
public sealed record ToolInvocation(string Name, string Arguments);

/// <summary>
/// Cienki adapter narzędzi udostępnianych modelowi AI.
/// Waliduje parametry wejściowe (NIP, rachunek, imię i nazwisko), wywołuje <see cref="ICounterpartyVerifier"/>
/// i formatuje wynik przez <see cref="ContextBuilder"/>.
/// </summary>
/// <remarks>
/// Narzędzia nie rzucają wyjątków do modelu: błędne dane wejściowe i awarie źródeł wracają jako czytelny tekst,
/// który model może przekazać użytkownikowi.
/// </remarks>
public sealed class RegistryTools(
    ICounterpartyVerifier verifier,
    ContextBuilder contextBuilder)
{
    public const string CheckCounterpartyName = "sprawdz_kontrahenta";
    public const string CheckAccountName = "sprawdz_rachunek";
    public const string FindPersonLinksName = "znajdz_powiazania_osoby";

    public event Action<ToolInvocation>? Invoked;

    public IList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(
            CheckCounterpartyAsync,
            name: CheckCounterpartyName,
            description: """
                Sprawdza polską firmę po NIP-ie we wszystkich rejestrach (Biała lista VAT, KRS, CRBR, lista sankcyjna UE,
                powiązania osobowe) i zwraca ocenę ryzyka wyliczoną przez program wraz z faktami, źródłami i zaleceniami.
                Używaj zawsze, gdy użytkownik podaje NIP firmy, o którą pyta. Jeśli podał numer rachunku z faktury, przekaż go też.
                """),

        AIFunctionFactory.Create(
            CheckAccountAsync,
            name: CheckAccountName,
            description: """
                Sprawdza na Białej liście VAT, czy numer rachunku bankowego jest przypisany do firmy o podanym NIP-ie.
                Używaj, gdy użytkownik pyta o rachunek do firmy, którą już sprawdziłeś.
                """),

        AIFunctionFactory.Create(
            FindPersonLinksAsync,
            name: FindPersonLinksName,
            description: """
                Wyszukuje firmy, w których zarządzie zasiada dana osoba (na podstawie KRS). Zwraca nazwy firm, NIP-y i funkcje.
                Używaj, gdy użytkownik pyta o powiązania osoby z zarządu lub beneficjenta z innymi firmami.
                """)
    ];

    public async Task<string> CheckCounterpartyAsync(
        [Description("NIP firmy: 10 cyfr, mogą być myślniki, np. 123-456-78-90.")] string nip,
        [Description("Numer rachunku bankowego z faktury (26 cyfr). Podaj tylko wtedy, gdy użytkownik go podał.")] string? numerRachunku = null,
        CancellationToken cancellationToken = default)
    {
        Invoked?.Invoke(new ToolInvocation(CheckCounterpartyName,
            string.IsNullOrWhiteSpace(numerRachunku) ? $"nip: {nip}" : $"nip: {nip}, rachunek: {numerRachunku}"));

        if (!Nip.TryParse(nip, out var parsedNip, out var nipError))
            return $"BŁĄD: {nipError} Nie sprawdzono żadnego rejestru. Poproś użytkownika o sprawdzenie NIP-u.";

        BankAccountNumber? account = null;
        string? accountWarning = null;

        if (!string.IsNullOrWhiteSpace(numerRachunku))
        {
            if (BankAccountNumber.TryParse(numerRachunku, out var parsedAccount, out var accountError))
                account = parsedAccount;
            else
                accountWarning = $"UWAGA: {accountError} Rachunku nie sprawdzono – poproś użytkownika o poprawny numer.";
        }

        var report = await verifier.VerifyAsync(parsedNip, account, cancellationToken);
        var context = contextBuilder.Build(report);

        return accountWarning is null ? context : $"{accountWarning}\n{context}";
    }

    public async Task<string> CheckAccountAsync(
        [Description("NIP firmy: 10 cyfr.")] string nip,
        [Description("Numer rachunku bankowego (26 cyfr, może zawierać spacje i prefiks PL).")] string numerRachunku,
        CancellationToken cancellationToken = default)
    {
        Invoked?.Invoke(new ToolInvocation(CheckAccountName, $"nip: {nip}, rachunek: {numerRachunku}"));

        if (!Nip.TryParse(nip, out var parsedNip, out var nipError))
            return $"BŁĄD: {nipError}";

        if (!BankAccountNumber.TryParse(numerRachunku, out var account, out var accountError))
            return $"BŁĄD: {accountError}";

        var check = await verifier.CheckAccountAsync(parsedNip, account, cancellationToken);
        return contextBuilder.BuildAccountCheck(parsedNip, check);
    }

    public async Task<string> FindPersonLinksAsync(
        [Description("Imię i nazwisko osoby, np. Jan Kowalski.")] string imieINazwisko,
        CancellationToken cancellationToken = default)
    {
        Invoked?.Invoke(new ToolInvocation(FindPersonLinksName, $"osoba: {imieINazwisko}"));

        if (string.IsNullOrWhiteSpace(imieINazwisko) || imieINazwisko.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            return "BŁĄD: podaj imię i nazwisko osoby (co najmniej dwa słowa).";

        var check = await verifier.FindPersonLinksAsync(imieINazwisko, cancellationToken);
        return contextBuilder.BuildPersonLinks(imieINazwisko, check);
    }
}
