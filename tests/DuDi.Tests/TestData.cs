using DuDi.Core;
using DuDi.Core.Verification;
using DuDi.Sources.Mock;

namespace DuDi.Tests;

/// <summary>NIP-y firm z symulowanej bazy (data/fixtures). Wszystkie fikcyjne, sprawdzone, że nie należą do prawdziwych firm.</summary>
public static class MockNips
{
    public static readonly Nip Alfa = Nip.Parse("9488127353");
    public static readonly Nip Beta = Nip.Parse("9255757334");
    public static readonly Nip Gamma = Nip.Parse("9101438731");
    public static readonly Nip Delta = Nip.Parse("2650172391");
    public static readonly Nip Epsilon = Nip.Parse("5409153639");
    public static readonly Nip Zeta = Nip.Parse("8097202819");
    public static readonly Nip Eta = Nip.Parse("7181952250");
    public static readonly Nip Iota = Nip.Parse("4597795631");

    /// <summary>Poprawny NIP spoza bazy.</summary>
    public static readonly Nip Unknown = Nip.Parse("1032860545");

    /// <summary>Rachunek Bety widoczny na Białej liście.</summary>
    public static readonly BankAccountNumber BetaAccount = BankAccountNumber.Parse("70105014453872590736196366");

    /// <summary>Rachunek z „faktury” Bety, którego nie ma na Białej liście.</summary>
    public static readonly BankAccountNumber BetaInvoiceAccountNotOnList = BankAccountNumber.Parse("20124010379198662181871482");
}

/// <summary>Stała data „dziś” – wyniki reguł (np. terminy sprawozdań, wiek firmy) nie zależą od dnia uruchomienia testów.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly FixedTimeProvider Checkpoint = new(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public static class TestEnvironment
{
    public static string DataPath(params string[] parts) =>
        Path.Combine([AppContext.BaseDirectory, "data", .. parts]);

    public static MockRegistrySource CreateMockSource(params string[] unavailableSources) => new(new MockDataOptions
    {
        FixturesPath = DataPath("fixtures"),
        UnavailableSources = [.. unavailableSources]
    });

    public static CounterpartyVerifier CreateVerifier(params string[] unavailableSources) =>
        new(CreateMockSource(unavailableSources), FixedTimeProvider.Checkpoint);
}
