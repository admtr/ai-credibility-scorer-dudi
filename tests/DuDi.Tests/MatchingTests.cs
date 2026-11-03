using DuDi.Agent;
using DuDi.Core;

namespace DuDi.Tests;

public class NameMatcherTests
{
    [Theory]
    [InlineData("Jan Kowalski", "JAN KOWALSKI")]
    [InlineData("Jan Kowalski", "Kowalski Jan")]
    [InlineData("Arkadij Zmyślonow", "ARKADIJ ZMYSLONOW")]
    [InlineData("Paweł Łoś", "PAWEL LOS")]
    [InlineData("Jan Kowalski", "Jan Maria Kowalski")]
    public void Matches_same_person_written_differently(string first, string second)
    {
        Assert.True(NameMatcher.Matches(first, second));
    }

    [Theory]
    [InlineData("Jan Kowalski", "Jan Nowak")]
    [InlineData("Kowalski", "Jan Kowalski")]           // jedno słowo to za mało, żeby uznać trafienie
    [InlineData("", "Jan Kowalski")]
    [InlineData(null, "Jan Kowalski")]
    public void Does_not_match_different_or_too_short_names(string? first, string second)
    {
        Assert.False(NameMatcher.Matches(first, second));
    }
}

public class IdentifierDetectorTests
{
    [Fact]
    public void Finds_nip_written_with_dashes()
    {
        var detected = IdentifierDetector.Detect("Sprawdź firmę 948-812-73-53, proszę.");

        Assert.Equal(MockNips.Alfa, Assert.Single(detected.Nips));
        Assert.Empty(detected.Accounts);
    }

    [Fact]
    public void Finds_nip_and_invoice_account_without_confusing_them()
    {
        var detected = IdentifierDetector.Detect("NIP 9255757334, rachunek z faktury PL20 1240 1037 9198 6621 8187 1482");

        Assert.Equal(MockNips.Beta, Assert.Single(detected.Nips));
        Assert.Equal(MockNips.BetaInvoiceAccountNotOnList, Assert.Single(detected.Accounts));
        Assert.Empty(detected.InvalidNips);
    }

    [Fact]
    public void Reports_nip_with_invalid_checksum()
    {
        var detected = IdentifierDetector.Detect("Co z firmą 7740001455?");

        Assert.Empty(detected.Nips);
        Assert.Equal("7740001455", Assert.Single(detected.InvalidNips));
    }

    [Fact]
    public void Ignores_text_without_identifiers()
    {
        var detected = IdentifierDetector.Detect("Napisz mi wiersz o wiośnie.");

        Assert.Empty(detected.Nips);
        Assert.Empty(detected.Accounts);
        Assert.Empty(detected.InvalidNips);
    }
}
