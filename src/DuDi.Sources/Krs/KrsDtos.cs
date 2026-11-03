using System.Text.Json;

namespace DuDi.Sources.Krs;

// Format odpowiedzi API KRS: GET https://api-krs.ms.gov.pl/api/krs/OdpisAktualny/{KRS}?rejestr=P&format=json
// Tylko pola potrzebne do oceny; strukturę sprawdziliśmy na prawdziwych odpisach w data/samples/.
// Uwaga: publiczne API maskuje imiona, nazwiska i PESEL osób (np. "F*****").

public sealed class KrsOdpisResponse
{
    public KrsOdpis? Odpis { get; set; }
}

public sealed class KrsOdpis
{
    public string? Rodzaj { get; set; }
    public KrsNaglowek? NaglowekA { get; set; }
    public KrsDane? Dane { get; set; }
}

public sealed class KrsNaglowek
{
    public string? Rejestr { get; set; }
    public string? NumerKRS { get; set; }
    public string? DataCzasOdpisu { get; set; }
    public string? StanZDnia { get; set; }
    public string? DataRejestracjiWKRS { get; set; }
}

public sealed class KrsDane
{
    public KrsDzial1? Dzial1 { get; set; }
    public KrsDzial2? Dzial2 { get; set; }
    public KrsDzial3? Dzial3 { get; set; }
    public KrsDzial6? Dzial6 { get; set; }
}

/// <summary>Dział 1: dane podmiotu.</summary>
public sealed class KrsDzial1
{
    public KrsDanePodmiotu? DanePodmiotu { get; set; }
}

public sealed class KrsDanePodmiotu
{
    public string? FormaPrawna { get; set; }
    public KrsIdentyfikatory? Identyfikatory { get; set; }
    public string? Nazwa { get; set; }
}

public sealed class KrsIdentyfikatory
{
    public string? Regon { get; set; }
    public string? Nip { get; set; }
}

/// <summary>Dział 2: organy reprezentacji (zarząd) i prokurenci.</summary>
public sealed class KrsDzial2
{
    public KrsOrgan? Reprezentacja { get; set; }
}

public sealed class KrsOrgan
{
    public string? NazwaOrganu { get; set; }
    public string? SposobReprezentacji { get; set; }
    public List<KrsOsoba>? Sklad { get; set; }
}

public sealed class KrsOsoba
{
    public KrsNazwisko? Nazwisko { get; set; }
    public KrsImiona? Imiona { get; set; }
    public string? FunkcjaWOrganie { get; set; }
    public bool? CzyZawieszona { get; set; }
}

public sealed class KrsNazwisko
{
    public string? NazwiskoICzlon { get; set; }
    public string? NazwiskoCzlonDrugi { get; set; }
}

public sealed class KrsImiona
{
    public string? Imie { get; set; }
    public string? ImieDrugie { get; set; }
}

/// <summary>Dział 3: m.in. wzmianki o złożonych sprawozdaniach finansowych.</summary>
public sealed class KrsDzial3
{
    public KrsWzmianki? WzmiankiOZlozonychDokumentach { get; set; }
}

public sealed class KrsWzmianki
{
    public List<KrsWzmianka>? WzmiankaOZlozeniuRocznegoSprawozdaniaFinansowego { get; set; }
}

public sealed class KrsWzmianka
{
    /// <summary>„16.06.2026”.</summary>
    public string? DataZlozenia { get; set; }

    /// <summary>„OD 01.01.2025 DO 31.12.2025”.</summary>
    public string? ZaOkresOdDo { get; set; }
}

/// <summary>Dział 6: m.in. likwidacja, upadłość, postępowania restrukturyzacyjne.</summary>
public sealed class KrsDzial6
{
    public List<KrsPostepowanieUpadlosciowe>? PostepowanieUpadlosciowe { get; set; }

    /// <summary>
    /// Struktura sekcji likwidacji nie została jeszcze potwierdzona na prawdziwym odpisie, dlatego czytamy ją
    /// jako dowolny JSON i sprawdzamy tylko, czy jest niepusta (dodatkowo mapper patrzy na dopisek „W LIKWIDACJI” w nazwie).
    /// </summary>
    public JsonElement? Likwidacja { get; set; }
}

public sealed class KrsPostepowanieUpadlosciowe
{
    public KrsOrzeczenie? InformacjaOOgloszeniuUpadlosci { get; set; }

    /// <summary>Pusty obiekt <c>{}</c>, dopóki postępowanie trwa.</summary>
    public JsonElement? OpisZakonczeniaProcesuUpadlosci { get; set; }

    public string? SposobProwadzeniaPostepowania { get; set; }
}

public sealed class KrsOrzeczenie
{
    public string? OrganWydajacy { get; set; }
    public string? Sygnatura { get; set; }
    public string? Data { get; set; }
}
