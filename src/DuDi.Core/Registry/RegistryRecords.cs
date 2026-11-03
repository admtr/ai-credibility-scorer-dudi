namespace DuDi.Core.Registry;

// Znormalizowane dane z rejestrów. Źródła (mock lub HTTP) mapują na nie surowe odpowiedzi API,
// więc reguły oceny i agent nie zależą od formatu konkretnego rejestru.

public enum VatStatus
{
    /// <summary>„Czynny” – zarejestrowany czynny podatnik VAT.</summary>
    Active,

    /// <summary>„Zwolniony” – podatnik zwolniony z VAT.</summary>
    Exempt,

    /// <summary>„Niezarejestrowany” lub wykreślony z rejestru VAT.</summary>
    NotRegistered
}

/// <summary>Podmiot z wykazu podatników VAT (Biała lista).</summary>
public sealed record VatSubject(
    Nip Nip,
    string Name,
    VatStatus Status,
    string? KrsNumber,
    string? Regon,
    string? Address,
    DateOnly? RegistrationLegalDate,
    DateOnly? RemovalDate,
    string? RemovalBasis,
    IReadOnlyList<BankAccountNumber> AccountNumbers);

/// <summary>Członek organu reprezentującego (zarządu) z działu 2 KRS.</summary>
public sealed record BoardMember(string FullName, string Function)
{
    /// <summary>Publiczne API KRS maskuje imiona i nazwiska gwiazdkami (np. „J*** K*******”).</summary>
    public bool IsMasked => FullName.Contains('*');
}

/// <summary>Wzmianka o złożeniu rocznego sprawozdania finansowego (dział 3 KRS).</summary>
public sealed record FinancialStatementFiling(int FiscalYear, DateOnly FiledOn);

/// <summary>Ogłoszona upadłość (dział 6 KRS).</summary>
public sealed record BankruptcyInfo(DateOnly? DeclaredOn, string? Court, string? CaseNumber);

/// <summary>Wyciąg z odpisu aktualnego KRS – tylko pola potrzebne do oceny.</summary>
public sealed record KrsEntry(
    string KrsNumber,
    string Name,
    string? LegalForm,
    Nip? Nip,
    DateOnly? RegisteredOn,
    IReadOnlyList<BoardMember> Board,
    IReadOnlyList<FinancialStatementFiling> FinancialStatements,
    BankruptcyInfo? Bankruptcy,
    bool InLiquidation);

/// <summary>Beneficjent rzeczywisty z CRBR.</summary>
public sealed record BeneficialOwner(string FullName, string? Citizenship, string? Ownership);

/// <summary>Zgłoszenie w CRBR.</summary>
public sealed record BeneficialOwnersInfo(IReadOnlyList<BeneficialOwner> Owners, DateOnly? ReportedOn);

/// <summary>Trafienie na liście sankcyjnej.</summary>
public sealed record SanctionHit(string QueriedName, string ListedName, string? Programme, string? Reference);

/// <summary>Firma, w której zarządzie zasiada dana osoba.</summary>
public sealed record CompanyLink(Nip Nip, string CompanyName, string PersonName, string Role);
