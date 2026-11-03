using DuDi.Core.Registry;
using DuDi.Core.Risk;

namespace DuDi.Core.Verification;

public enum CheckStatus
{
    /// <summary>Źródło odpowiedziało i ma dane.</summary>
    Ok,

    /// <summary>Źródło odpowiedziało, ale nie ma danych (np. brak wpisu w CRBR).</summary>
    NotFound,

    /// <summary>Źródło nie odpowiedziało.</summary>
    Unavailable,

    /// <summary>Sprawdzenie nie dotyczy tego podmiotu (np. CRBR dla firmy spoza KRS).</summary>
    NotApplicable
}

/// <summary>Wynik zapytania do jednego źródła wraz z pochodzeniem: skąd i kiedy.</summary>
public sealed record SourceCheck<T>(
    string Source,
    DateOnly CheckedOn,
    CheckStatus Status,
    T? Value = null,
    string? Note = null)
    where T : class;

public sealed record AccountCheck(BankAccountNumber Account, bool Assigned);

public sealed record SanctionsCheck(IReadOnlyList<string> CheckedNames, IReadOnlyList<SanctionHit> Hits);

/// <summary>Powiązana firma wraz z jej wstępnym werdyktem (liczonym bez dalszego zagłębiania się w powiązania).</summary>
public sealed record RelatedCompany(CompanyLink Link, RiskLevel? Verdict);

public sealed record RelationsCheck(IReadOnlyList<RelatedCompany> Companies);

/// <summary>Wszystkie fakty o kontrahencie zebrane z rejestrów – wejście dla <see cref="RiskScorer"/>.</summary>
public sealed record CompanyProfile(
    Nip Nip,
    string? Name,
    DateOnly CheckedOn,
    SourceCheck<VatSubject> Vat,
    SourceCheck<AccountCheck>? Account,
    SourceCheck<KrsEntry> Krs,
    SourceCheck<BeneficialOwnersInfo> BeneficialOwners,
    SourceCheck<SanctionsCheck> Sanctions,
    SourceCheck<RelationsCheck> Relations)
{
    public bool IsKnown => Vat.Status == CheckStatus.Ok || Krs.Status == CheckStatus.Ok;
}
