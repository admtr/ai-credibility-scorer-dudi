using DuDi.Core;
using DuDi.Core.Registry;

namespace DuDi.Sources.WhiteList;

/// <summary>Surowa odpowiedź Białej listy → <see cref="VatSubject"/>. Ten sam mapper posłuży klientowi HTTP w etapie 3.</summary>
public static class WhiteListMapper
{
    public static VatSubject? ToVatSubject(WhiteListSearchResponse response)
    {
        if (response.Result?.Subject is not { } subject)
            return null;

        if (!Nip.TryParse(subject.Nip, out var nip, out var error))
            throw new InvalidDataException($"Biała lista zwróciła niepoprawny NIP: {error}");

        var accounts = (subject.AccountNumbers ?? [])
            .Select(a => BankAccountNumber.TryParse(a, out var account) ? account : (BankAccountNumber?)null)
            .OfType<BankAccountNumber>()
            .ToList();

        return new VatSubject(
            nip,
            subject.Name ?? "(brak nazwy)",
            ParseStatus(subject.StatusVat),
            string.IsNullOrWhiteSpace(subject.Krs) ? null : subject.Krs,
            subject.Regon,
            subject.WorkingAddress ?? subject.ResidenceAddress,
            RegistryJson.ParseIsoDate(subject.RegistrationLegalDate),
            RegistryJson.ParseIsoDate(subject.RemovalDate),
            subject.RemovalBasis,
            accounts);
    }

    public static VatStatus ParseStatus(string? statusVat) => statusVat?.Trim().ToUpperInvariant() switch
    {
        "CZYNNY" => VatStatus.Active,
        "ZWOLNIONY" => VatStatus.Exempt,
        _ => VatStatus.NotRegistered
    };

    public static bool IsAccountAssigned(WhiteListCheckResponse response) =>
        string.Equals(response.Result?.AccountAssigned, "TAK", StringComparison.OrdinalIgnoreCase);
}
