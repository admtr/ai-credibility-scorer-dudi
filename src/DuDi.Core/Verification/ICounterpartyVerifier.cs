using DuDi.Core.Registry;
using DuDi.Core.Risk;

namespace DuDi.Core.Verification;

/// <summary>
/// Kontrakt warstwy aplikacyjnej/domenowej obsługujący weryfikację kontrahentów, rachunków bankowych i powiązań osobowych.
/// </summary>
public interface ICounterpartyVerifier
{
    /// <summary>Pełna weryfikacja kontrahenta we wszystkich rejestrach wraz z wyliczeniem raportu ryzyka.</summary>
    Task<RiskReport> VerifyAsync(Nip nip, BankAccountNumber? invoiceAccount = null, CancellationToken cancellationToken = default);

    /// <summary>Sprawdzenie na Białej liście VAT, czy rachunek bankowy jest przypisany do podmiotu o podanym NIP-ie.</summary>
    Task<SourceCheck<AccountCheck>> CheckAccountAsync(Nip nip, BankAccountNumber account, CancellationToken cancellationToken = default);

    /// <summary>Wyszukanie spółek, w których zarządzie zasiada osoba o podanym imieniu i nazwisku.</summary>
    Task<SourceCheck<IReadOnlyList<CompanyLink>>> FindPersonLinksAsync(string fullName, CancellationToken cancellationToken = default);
}
