using DuDi.Core;

namespace DuDi.Tests;

public class NipTests
{
    [Theory]
    [InlineData("7740001454")]          // Orlen – prawdziwy NIP z próbek API
    [InlineData("774-000-14-54")]
    [InlineData("774-00-01-454")]
    [InlineData("PL7740001454")]
    [InlineData("  774 000 14 54 ")]
    public void Accepts_valid_nip_in_common_formats(string input)
    {
        Assert.True(Nip.TryParse(input, out var nip));
        Assert.Equal("7740001454", nip.Value);
    }

    [Theory]
    [InlineData("7740001455")]          // literówka w ostatniej cyfrze
    [InlineData("7740001545")]          // zamienione dwie cyfry
    [InlineData("1234567890")]          // reszta z sumy ważonej = 10, taki NIP nie istnieje
    public void Rejects_nip_with_invalid_checksum(string input)
    {
        Assert.False(Nip.TryParse(input, out _, out var error));
        Assert.Contains("sumę kontrolną", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("774000145")]           // 9 cyfr
    [InlineData("77400014541")]         // 11 cyfr
    [InlineData("774O001454")]          // litera O zamiast zera
    [InlineData("774/000/14/54")]
    public void Rejects_input_that_is_not_a_nip(string input)
    {
        Assert.False(Nip.TryParse(input, out _, out var error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("1234567891")]
    [InlineData("1234567899")]
    public void Rejects_nip_whose_checksum_would_be_ten(string input)
    {
        // Suma ważona 9 cyfr daje resztę 10 – taki numer nie może zostać nadany, niezależnie od ostatniej cyfry.
        Assert.False(Nip.TryParse(input, out _));
    }

    [Fact]
    public void Formats_nip_for_display()
    {
        Assert.Equal("774-000-14-54", Nip.Parse("7740001454").ToDisplayString());
    }

    [Fact]
    public void All_mock_nips_are_valid()
    {
        // Gdyby ktoś przekręcił NIP w danych testowych, Parse rzuciłby wyjątek już przy inicjalizacji MockNips.
        Assert.Equal("9488127353", MockNips.Alfa.Value);
        Assert.Equal("1032860545", MockNips.Unknown.Value);
    }
}
