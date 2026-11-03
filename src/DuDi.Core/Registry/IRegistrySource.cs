namespace DuDi.Core.Registry;

/// <summary>
/// Dostęp do publicznych rejestrów. Na tym etapie jedyną implementacją jest symulowana baza
/// (<c>MockRegistrySource</c>); w etapach 3–4 podstawimy za ten interfejs klientów HTTP bez zmian w agencie i regułach.
/// </summary>
/// <remarks>
/// Metody zwracają <c>null</c> (lub pustą listę), gdy rejestr odpowiedział, ale nie ma danych.
/// Gdy rejestr nie odpowiedział, rzucają <see cref="SourceUnavailableException"/> – to osobny wynik, nigdy „wszystko OK”.
/// </remarks>
public interface IRegistrySource
{
    /// <summary>Biała lista VAT: wyszukiwanie podmiotu po NIP według stanu na dany dzień.</summary>
    Task<VatSubject?> GetVatSubjectAsync(Nip nip, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Biała lista VAT: czy rachunek jest przypisany do podmiotu (endpoint <c>check</c>).</summary>
    Task<bool> IsAccountAssignedAsync(Nip nip, BankAccountNumber account, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>KRS: odpis aktualny po numerze KRS (numer bierzemy z Białej listy).</summary>
    Task<KrsEntry?> GetKrsEntryAsync(string krsNumber, CancellationToken cancellationToken = default);

    /// <summary>CRBR: beneficjenci rzeczywiści.</summary>
    Task<BeneficialOwnersInfo?> GetBeneficialOwnersAsync(Nip nip, CancellationToken cancellationToken = default);

    /// <summary>Lista sankcyjna: trafienia dla nazwy firmy lub osoby.</summary>
    Task<IReadOnlyList<SanctionHit>> CheckSanctionsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Firmy, w których zarządzie zasiada osoba o podanym imieniu i nazwisku.</summary>
    Task<IReadOnlyList<CompanyLink>> FindCompaniesByPersonAsync(string fullName, CancellationToken cancellationToken = default);
}
