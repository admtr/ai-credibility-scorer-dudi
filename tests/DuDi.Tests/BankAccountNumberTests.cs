using DuDi.Core;

namespace DuDi.Tests;

public class BankAccountNumberTests
{
    [Theory]
    [InlineData("30102010260000140203550589")]             // prawdziwy rachunek z próbki Białej listy
    [InlineData("30 1020 1026 0000 1402 0355 0589")]
    [InlineData("PL30102010260000140203550589")]
    [InlineData("PL 30 1020 1026 0000 1402 0355 0589")]
    public void Accepts_valid_account_in_common_formats(string input)
    {
        Assert.True(BankAccountNumber.TryParse(input, out var account));
        Assert.Equal("30102010260000140203550589", account.Value);
    }

    [Theory]
    [InlineData("31102010260000140203550589")]             // zmieniona cyfra kontrolna
    [InlineData("30102010260000140203550598")]             // zamienione dwie ostatnie cyfry
    public void Rejects_account_with_invalid_checksum(string input)
    {
        Assert.False(BankAccountNumber.TryParse(input, out _, out var error));
        Assert.Contains("sumę kontrolną", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3010201026000014020355058")]              // 25 cyfr
    [InlineData("DE89370400440532013000")]                 // IBAN niemiecki
    public void Rejects_input_that_is_not_polish_account(string input)
    {
        Assert.False(BankAccountNumber.TryParse(input, out _));
    }

    [Fact]
    public void Formats_account_for_display()
    {
        Assert.Equal("30 1020 1026 0000 1402 0355 0589", BankAccountNumber.Parse("30102010260000140203550589").ToDisplayString());
    }
}
