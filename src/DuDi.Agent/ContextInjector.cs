using System.Text;
using DuDi.Core;
using DuDi.Core.Verification;

namespace DuDi.Agent;

/// <summary>
/// Tryb <see cref="ContextMode.Injected"/>: to aplikacja, a nie model, decyduje, jakie dane pobrać.
/// Rozpoznaje NIP w wiadomości, pobiera dane i dokleja blok &lt;dane_kontrahenta&gt; przed treścią użytkownika.
/// Zostawiamy go do porównania z tool callingiem (etap 2 vs etap 3) i do ewaluacji.
/// </summary>
public sealed class ContextInjector(CounterpartyVerifier verifier, ContextBuilder contextBuilder)
{
    public async Task<string> EnrichAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var detected = IdentifierDetector.Detect(userMessage);

        if (detected.Nips.Count == 0 && detected.InvalidNips.Count == 0)
            return userMessage;

        var sb = new StringBuilder();

        foreach (var invalid in detected.InvalidNips)
            sb.AppendLine($"<uwaga>Ciąg „{invalid}” nie jest poprawnym NIP-em (błędna suma kontrolna). Nie sprawdzano go w rejestrach.</uwaga>");

        // Rachunek z faktury przypisujemy do pierwszej firmy – zwykle użytkownik pyta o jedną fakturę naraz.
        BankAccountNumber? account = detected.Accounts.Count > 0 ? detected.Accounts[0] : null;

        for (var i = 0; i < detected.Nips.Count; i++)
        {
            var report = await verifier.VerifyAsync(detected.Nips[i], i == 0 ? account : null, cancellationToken);
            sb.AppendLine(contextBuilder.Build(report));
        }

        sb.AppendLine($"Wiadomość użytkownika: {userMessage}");
        return sb.ToString();
    }
}
