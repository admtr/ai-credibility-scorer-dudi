using DuDi.Core.Registry;

namespace DuDi.Sources.Crbr;

// CRBR (https://crbr.podatki.gov.pl) nie ma potwierdzonego publicznego API – wyszukiwarka to aplikacja webowa.
// Dlatego to jest WŁASNY, uproszczony format symulowanej bazy (pola nazwane jak w formularzu CRBR).
// Plan B na produkcję: dane z CRBR wprowadzane ręcznie albo link do ręcznej weryfikacji w raporcie.

public sealed class CrbrEntry
{
    public string? Nip { get; set; }
    public string? NazwaSpolki { get; set; }

    /// <summary>„2024-05-10”.</summary>
    public string? DataZgloszenia { get; set; }

    public List<CrbrBeneficjent>? Beneficjenci { get; set; }

    public BeneficialOwnersInfo ToBeneficialOwnersInfo() => new(
        (Beneficjenci ?? [])
            .Select(b => new BeneficialOwner($"{b.Imie} {b.Nazwisko}".Trim(), b.Obywatelstwo, b.RodzajUprawnien))
            .ToList(),
        RegistryJson.ParseIsoDate(DataZgloszenia));
}

public sealed class CrbrBeneficjent
{
    public string? Imie { get; set; }
    public string? Nazwisko { get; set; }
    public string? Obywatelstwo { get; set; }
    public string? KrajZamieszkania { get; set; }

    /// <summary>Np. „Udziałowiec – 60% udziałów”.</summary>
    public string? RodzajUprawnien { get; set; }
}
