namespace DuDi.Sources.WhiteList;

// Format odpowiedzi API Białej listy VAT (https://wl-api.mf.gov.pl), zweryfikowany na prawdziwych odpowiedziach
// zapisanych w data/samples/. Pominięte pola są ignorowane przy deserializacji.

/// <summary>GET /api/search/nip/{nip}?date={yyyy-MM-dd}</summary>
public sealed class WhiteListSearchResponse
{
    public WhiteListSearchResult? Result { get; set; }
}

public sealed class WhiteListSearchResult
{
    /// <summary><c>null</c>, gdy podmiotu nie ma w wykazie.</summary>
    public WhiteListSubject? Subject { get; set; }

    public string? RequestDateTime { get; set; }

    public string? RequestId { get; set; }
}

public sealed class WhiteListSubject
{
    public string? Name { get; set; }
    public string? Nip { get; set; }

    /// <summary>„Czynny”, „Zwolniony” albo „Niezarejestrowany”.</summary>
    public string? StatusVat { get; set; }

    public string? Regon { get; set; }
    public string? Krs { get; set; }
    public string? ResidenceAddress { get; set; }
    public string? WorkingAddress { get; set; }
    public string? RegistrationLegalDate { get; set; }
    public string? RegistrationDenialDate { get; set; }
    public string? RemovalDate { get; set; }
    public string? RemovalBasis { get; set; }
    public string? RestorationDate { get; set; }
    public List<string>? AccountNumbers { get; set; }
}

/// <summary>GET /api/check/nip/{nip}/bank-account/{account}?date={yyyy-MM-dd}</summary>
public sealed class WhiteListCheckResponse
{
    public WhiteListCheckResult? Result { get; set; }
}

public sealed class WhiteListCheckResult
{
    /// <summary>„TAK” albo „NIE”.</summary>
    public string? AccountAssigned { get; set; }

    public string? RequestDateTime { get; set; }

    public string? RequestId { get; set; }
}

/// <summary>Odpowiedź błędu, np. <c>{"message":"Nieprawidłowy NIP.","code":"WL-115"}</c>.</summary>
public sealed class WhiteListError
{
    public string? Message { get; set; }
    public string? Code { get; set; }
}
