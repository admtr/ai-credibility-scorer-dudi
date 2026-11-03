using DuDi.Core;
using DuDi.Core.Registry;
using DuDi.Core.Risk;
using DuDi.Core.Verification;

namespace DuDi.Tests;

/// <summary>Reguły na ręcznie zbudowanych profilach – bez plików i bez źródeł danych.</summary>
public class RiskScorerTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);

    [Theory]
    [InlineData(2026, 1, 10, 2024)]
    [InlineData(2026, 7, 15, 2024)]     // ostatni dzień terminu – sprawozdanie za 2025 może jeszcze nie wpłynąć
    [InlineData(2026, 7, 16, 2025)]
    [InlineData(2026, 11, 2, 2025)]
    public void Latest_required_fiscal_year_follows_july_15_deadline(int year, int month, int day, int expected)
    {
        Assert.Equal(expected, RiskScorer.LatestRequiredFiscalYear(new DateOnly(year, month, day)));
    }

    [Fact]
    public void Verdict_is_the_highest_severity_among_findings()
    {
        var profile = Profile(
            vat: Vat(VatStatus.Exempt),
            krs: Krs(registeredOn: new DateOnly(2010, 1, 1), statements: [2024, 2025], bankruptcy: new BankruptcyInfo(Today, null, null)));

        var report = RiskScorer.Score(profile);

        Assert.Equal(RiskLevel.High, report.Verdict);
        Assert.Equal(RiskLevel.High, report.Findings[0].Severity);
        Assert.Equal(RiskLevel.Low, report.Findings[^1].Severity);
    }

    [Fact]
    public void No_findings_means_low_risk()
    {
        var report = RiskScorer.Score(Profile());

        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.Empty(report.Findings);
    }

    [Fact]
    public void Company_registered_in_second_half_of_year_may_skip_first_statement()
    {
        // Rejestracja w sierpniu 2024: pierwszy rok obrotowy może trwać do końca 2025, więc za 2024 sprawozdania nie ma.
        var report = RiskScorer.Score(Profile(krs: Krs(registeredOn: new DateOnly(2024, 8, 1), statements: [2025])));

        Assert.DoesNotContain(report.Findings, f => f.Code == FindingCodes.FinancialStatementsMissing);
    }

    [Fact]
    public void Missing_latest_statement_is_medium_risk()
    {
        var report = RiskScorer.Score(Profile(krs: Krs(registeredOn: new DateOnly(2010, 1, 1), statements: [2023, 2024])));

        var finding = Assert.Single(report.Findings);
        Assert.Equal(FindingCodes.FinancialStatementsMissing, finding.Code);
        Assert.Equal(RiskLevel.Medium, finding.Severity);
        Assert.Contains("2025", finding.Description);
    }

    [Fact]
    public void Liquidation_is_high_risk()
    {
        var report = RiskScorer.Score(Profile(krs: Krs(registeredOn: new DateOnly(2010, 1, 1), statements: [2024, 2025], inLiquidation: true)));

        Assert.Equal(FindingCodes.Liquidation, Assert.Single(report.Findings).Code);
        Assert.Equal(RiskLevel.High, report.Verdict);
    }

    [Fact]
    public void Company_younger_than_twelve_months_gets_information()
    {
        var report = RiskScorer.Score(Profile(krs: Krs(registeredOn: Today.AddMonths(-11), statements: [])));

        var finding = Assert.Single(report.Findings);
        Assert.Equal(FindingCodes.YoungCompany, finding.Code);
        Assert.Equal(RiskLevel.Low, finding.Severity);
    }

    [Fact]
    public void Unavailable_source_adds_no_finding_but_is_listed_as_not_checked()
    {
        var unavailable = new SourceCheck<BeneficialOwnersInfo>(SourceNames.Crbr, Today, CheckStatus.Unavailable, Note: "timeout");

        var report = RiskScorer.Score(Profile() with { BeneficialOwners = unavailable });

        Assert.Empty(report.Findings);
        Assert.True(report.IsIncomplete);
        var check = Assert.Single(report.Checks, c => c.Outcome == CheckOutcome.NotChecked);
        Assert.Contains("timeout", check.Summary);
    }

    [Fact]
    public void Sanctions_hit_is_critical()
    {
        var sanctions = new SourceCheck<SanctionsCheck>(SourceNames.Sanctions, Today, CheckStatus.Ok,
            new SanctionsCheck(["Jan Testowy"], [new SanctionHit("Jan Testowy", "TESTOWY Jan", "RUS", "EU.1.1")]));

        var report = RiskScorer.Score(Profile() with { Sanctions = sanctions });

        Assert.Equal(RiskLevel.Critical, report.Verdict);
    }

    private static readonly Nip TestNip = Nip.Parse("9488127353");

    private static CompanyProfile Profile(VatSubject? vat = null, KrsEntry? krs = null)
    {
        vat ??= Vat(VatStatus.Active);
        krs ??= Krs(registeredOn: new DateOnly(2010, 1, 1), statements: [2024, 2025]);

        return new CompanyProfile(
            TestNip,
            vat.Name,
            Today,
            new SourceCheck<VatSubject>(SourceNames.WhiteList, Today, CheckStatus.Ok, vat),
            null,
            new SourceCheck<KrsEntry>(SourceNames.Krs, Today, CheckStatus.Ok, krs),
            new SourceCheck<BeneficialOwnersInfo>(SourceNames.Crbr, Today, CheckStatus.Ok,
                new BeneficialOwnersInfo([new BeneficialOwner("Jan Testowy", "POLSKIE", "100% udziałów")], Today)),
            new SourceCheck<SanctionsCheck>(SourceNames.Sanctions, Today, CheckStatus.Ok, new SanctionsCheck(["Jan Testowy"], [])),
            new SourceCheck<RelationsCheck>(SourceNames.Relations, Today, CheckStatus.Ok, new RelationsCheck([])));
    }

    private static VatSubject Vat(VatStatus status) =>
        new(TestNip, "TESTOWA SPÓŁKA Z O.O.", status, "0009990001", null, null, new DateOnly(2010, 1, 1), null, null, []);

    private static KrsEntry Krs(DateOnly registeredOn, int[] statements, BankruptcyInfo? bankruptcy = null, bool inLiquidation = false) =>
        new("0009990001", "TESTOWA SPÓŁKA Z O.O.", null, TestNip, registeredOn,
            [new BoardMember("Jan Testowy", "Prezes zarządu")],
            statements.Select(y => new FinancialStatementFiling(y, new DateOnly(y + 1, 6, 30))).ToList(),
            bankruptcy,
            inLiquidation);
}
