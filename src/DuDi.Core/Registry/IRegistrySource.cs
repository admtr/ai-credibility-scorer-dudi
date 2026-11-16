namespace DuDi.Core.Registry;

/// <summary>
/// Port dostępu do Wykazu podatników VAT (Biała lista MF).
/// </summary>
public interface IVatWhiteListSource
{
    /// <summary>Biała lista VAT: wyszukiwanie podmiotu po NIP według stanu na dany dzień.</summary>
    Task<VatSubject?> GetVatSubjectAsync(Nip nip, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Biała lista VAT: czy rachunek jest przypisany do podmiotu (endpoint <c>check</c>).</summary>
    Task<bool> IsAccountAssignedAsync(Nip nip, BankAccountNumber account, DateOnly date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port dostępu do Krajowego Rejestru Sądowego (KRS).
/// </summary>
public interface IKrsSource
{
    /// <summary>KRS: odpis aktualny po numerze KRS (numer bierzemy z Białej listy).</summary>
    Task<KrsEntry?> GetKrsEntryAsync(string krsNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port dostępu do Centralnego Rejestru Beneficjentów Rzeczywistych (CRBR).
/// </summary>
public interface ICrbrSource
{
    /// <summary>CRBR: beneficjenci rzeczywiści.</summary>
    Task<BeneficialOwnersInfo?> GetBeneficialOwnersAsync(Nip nip, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port dostępu do list sankcyjnych (np. skonsolidowana lista sankcyjna UE).
/// </summary>
public interface ISanctionsSource
{
    /// <summary>Lista sankcyjna: trafienia dla nazwy firmy lub osoby.</summary>
    Task<IReadOnlyList<SanctionHit>> CheckSanctionsAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port wyszukiwania powiązań osobowych w zarządach spółek.
/// </summary>
public interface IPersonRelationsSource
{
    /// <summary>Firmy, w których zarządzie zasiada osoba o podanym imieniu i nazwisku.</summary>
    Task<IReadOnlyList<CompanyLink>> FindCompaniesByPersonAsync(string fullName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port katalogu firm demonstracyjnych (używany w trybie Mock do komendy <c>/firmy</c> i oznaczania danych testowych).
/// </summary>
public interface IDemoCompanyCatalog
{
    /// <summary>Lista firm dostępnych w bazie demonstracyjnej.</summary>
    IReadOnlyList<(Nip Nip, string Name)> Companies { get; }

    /// <summary>Nazwy źródeł z zasymulowaną awarią.</summary>
    IReadOnlyCollection<string> UnavailableSources { get; }
}

/// <summary>
/// Zbiorczy interfejs wszystkich rejestrów publicznych (wygodny w testach i dla symulowanej bazy <c>MockRegistrySource</c>).
/// Klasy aplikacyjne i domenowe powinny zależeć od wąskich portów (<see cref="IVatWhiteListSource"/>, <see cref="IKrsSource"/> itd.),
/// co pozwala podmieniać każde źródło osobno (np. HTTP dla Białej listy i KRS, plik CSV dla sankcji, Mock dla CRBR).
/// </summary>
/// <remarks>
/// Metody zwracają <c>null</c> (lub pustą listę), gdy rejestr odpowiedział, ale nie ma danych.
/// Gdy rejestr nie odpowiedział, rzucają <see cref="SourceUnavailableException"/> – to osobny wynik, nigdy „wszystko OK”.
/// </remarks>
public interface IRegistrySource :
    IVatWhiteListSource,
    IKrsSource,
    ICrbrSource,
    ISanctionsSource,
    IPersonRelationsSource;
