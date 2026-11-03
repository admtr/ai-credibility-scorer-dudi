using System.Text.Json;
using DuDi.Core.Registry;
using DuDi.Sources.Krs;
using DuDi.Sources.Sanctions;
using DuDi.Sources.WhiteList;

namespace DuDi.Tests;

/// <summary>
/// Prawdziwe odpowiedzi API pobrane 2026-11-03 (data/samples) przechodzą przez te same mappery co symulowana baza.
/// To dowód, że mock jest w formacie prawdziwych rejestrów i że przejście na HTTP nie wymaga zmian w mapowaniu.
/// </summary>
public class SampleParsingTests
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static T Load<T>(string file) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(TestEnvironment.DataPath("samples", file)), Json)!;

    [Fact]
    public void White_list_search_response_maps_to_vat_subject()
    {
        var subject = WhiteListMapper.ToVatSubject(Load<WhiteListSearchResponse>("whitelist-search-7740001454.json"));

        Assert.NotNull(subject);
        Assert.Equal("7740001454", subject.Nip.Value);
        Assert.Equal("ORLEN SPÓŁKA AKCYJNA", subject.Name);
        Assert.Equal(VatStatus.Active, subject.Status);
        Assert.Equal("0000028860", subject.KrsNumber);
        Assert.Equal(new DateOnly(1993, 7, 5), subject.RegistrationLegalDate);

        // Wszystkie 236 rachunków z prawdziwej odpowiedzi przechodzą walidację sumy kontrolnej.
        Assert.Equal(236, subject.AccountNumbers.Count);
    }

    [Fact]
    public void White_list_response_without_subject_maps_to_null()
    {
        Assert.Null(WhiteListMapper.ToVatSubject(Load<WhiteListSearchResponse>("whitelist-search-notfound.json")));
    }

    [Theory]
    [InlineData("whitelist-check-tak.json", true)]
    [InlineData("whitelist-check-nie.json", false)]
    public void White_list_check_response_maps_to_bool(string file, bool expected)
    {
        Assert.Equal(expected, WhiteListMapper.IsAccountAssigned(Load<WhiteListCheckResponse>(file)));
    }

    [Fact]
    public void White_list_error_response_has_code()
    {
        var error = Load<WhiteListError>("whitelist-error-invalid-nip.json");

        Assert.Equal("WL-115", error.Code);
    }

    [Fact]
    public void Krs_extract_maps_board_statements_and_registration_date()
    {
        var entry = KrsMapper.ToKrsEntry(Load<KrsOdpisResponse>("krs-odpis-0000028860.json"));

        Assert.Equal("0000028860", entry.KrsNumber);
        Assert.Equal("7740001454", entry.Nip?.Value);
        Assert.Equal(new DateOnly(2001, 7, 19), entry.RegisteredOn);
        Assert.Contains(entry.FinancialStatements, f => f.FiscalYear == 2025);
        Assert.Null(entry.Bankruptcy);
        Assert.False(entry.InLiquidation);

        // Publiczne API KRS maskuje dane osób – powiązań osobowych nie da się na nim sprawdzić.
        Assert.NotEmpty(entry.Board);
        Assert.All(entry.Board, m => Assert.True(m.IsMasked));
    }

    [Fact]
    public void Krs_extract_of_bankrupt_company_maps_bankruptcy()
    {
        var entry = KrsMapper.ToKrsEntry(Load<KrsOdpisResponse>("krs-odpis-0000304735-upadlosc.json"));

        Assert.NotNull(entry.Bankruptcy);
        Assert.Equal(new DateOnly(2023, 7, 20), entry.Bankruptcy.DeclaredOn);
        Assert.Equal("WA1M/GU/188/2023", entry.Bankruptcy.CaseNumber);
    }

    [Fact]
    public void Eu_sanctions_csv_sample_is_parsed_by_header_names()
    {
        var entries = EuSanctionsCsv.Parse(File.ReadAllText(TestEnvironment.DataPath("samples", "eu-sanctions-sample.csv")));

        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.WholeName == "Saddam Hussein Al-Tikriti");
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.LogicalId)));
    }

    [Fact]
    public void Csv_parser_handles_quoted_separators_and_quotes()
    {
        const string csv = "Entity_LogicalId;Entity_EU_ReferenceNumber;Entity_SubjectType;Entity_Regulation_Programme;NameAlias_WholeName\n" +
                           "1;EU.1;enterprise;RUS;\"Firma \"\"Alfa\"\"; Oddział\"\n";

        var entry = Assert.Single(EuSanctionsCsv.Parse(csv));

        Assert.Equal("Firma \"Alfa\"; Oddział", entry.WholeName);
    }
}
