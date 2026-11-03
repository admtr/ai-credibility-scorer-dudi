using DuDi.Core.Risk;
using DuDi.Core.Verification;
using DuDi.Sources.Mock;

namespace DuDi.Tests;

/// <summary>
/// Scenariusze z symulowanej bazy (plan, sekcja 3.4) przechodzą przez cały łańcuch:
/// pliki w formacie API → mappery → weryfikator → reguły. Werdykty muszą zgadzać się z tabelą scenariuszy.
/// </summary>
public class ScenarioTests
{
    private readonly CounterpartyVerifier verifier = TestEnvironment.CreateVerifier();

    [Fact]
    public async Task Alfa_clean_company_has_low_risk_and_no_findings()
    {
        var report = await verifier.VerifyAsync(MockNips.Alfa);

        Assert.True(report.CompanyFound);
        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.Empty(report.Findings);
        Assert.False(report.IsIncomplete);
    }

    [Fact]
    public async Task Beta_invoice_account_not_on_white_list_is_high_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Beta, MockNips.BetaInvoiceAccountNotOnList);

        Assert.Equal(RiskLevel.High, report.Verdict);
        Assert.Equal(FindingCodes.AccountNotOnWhiteList, Assert.Single(report.Findings).Code);
    }

    [Fact]
    public async Task Beta_with_account_from_white_list_is_low_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Beta, MockNips.BetaAccount);

        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.Contains(report.Checks, c => c.Outcome == CheckOutcome.Passed && c.Summary.Contains("jest na Białej liście"));
    }

    [Fact]
    public async Task Beta_without_account_reminds_that_account_was_not_checked()
    {
        var report = await verifier.VerifyAsync(MockNips.Beta);

        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.Contains(report.Checks, c => c.Outcome == CheckOutcome.Info && c.Summary.Contains("nie podano rachunku"));
    }

    [Fact]
    public async Task Gamma_missing_financial_statements_and_crbr_is_medium_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Gamma);

        Assert.Equal(RiskLevel.Medium, report.Verdict);
        Assert.Equal(
            new[] { FindingCodes.BeneficialOwnersMissing, FindingCodes.FinancialStatementsMissing },
            report.Findings.Select(f => f.Code).Order(StringComparer.Ordinal).ToArray());
        Assert.Contains("2024 i 2025", report.Findings.Single(f => f.Code == FindingCodes.FinancialStatementsMissing).Description);
    }

    [Fact]
    public async Task Delta_in_bankruptcy_is_high_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Delta);

        Assert.Equal(RiskLevel.High, report.Verdict);
        Assert.Equal(FindingCodes.Bankruptcy, report.Findings[0].Code);
        Assert.Contains("WA1M/GU/999/2026", report.Findings[0].Description);
        Assert.Contains(report.Findings, f => f.Code == FindingCodes.FinancialStatementsMissing);
    }

    [Fact]
    public async Task Epsilon_board_member_linked_to_bankrupt_company_is_medium_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Epsilon);

        Assert.Equal(RiskLevel.Medium, report.Verdict);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(FindingCodes.LinkedHighRisk, finding.Code);
        Assert.Contains("Tomasz Wójcik", finding.Description);
        Assert.Contains("DELTA LOGISTICS", finding.Description);

        var related = Assert.Single(report.Profile.Relations.Value!.Companies);
        Assert.Equal(MockNips.Delta, related.Link.Nip);
        Assert.Equal(RiskLevel.High, related.Verdict);
    }

    [Fact]
    public async Task Delta_link_to_low_risk_company_does_not_raise_its_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Delta);

        var related = Assert.Single(report.Profile.Relations.Value!.Companies);
        Assert.Equal(MockNips.Epsilon, related.Link.Nip);
        Assert.DoesNotContain(report.Findings, f => f.Code == FindingCodes.LinkedHighRisk);
    }

    [Fact]
    public async Task Zeta_sanctioned_beneficial_owner_is_critical()
    {
        var report = await verifier.VerifyAsync(MockNips.Zeta);

        Assert.Equal(RiskLevel.Critical, report.Verdict);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(FindingCodes.SanctionsHit, finding.Code);
        Assert.Contains("Arkadij Zmyślonow", finding.Description);
    }

    [Fact]
    public async Task Eta_young_vat_exempt_company_is_low_risk_with_information()
    {
        var report = await verifier.VerifyAsync(MockNips.Eta);

        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.Equal(
            new[] { FindingCodes.VatExempt, FindingCodes.YoungCompany },
            report.Findings.Select(f => f.Code).Order(StringComparer.Ordinal).ToArray());
        Assert.Contains(report.Checks, c => c.Summary.Contains("nie musiała jeszcze złożyć"));
    }

    [Fact]
    public async Task Iota_removed_from_vat_register_is_high_risk()
    {
        var report = await verifier.VerifyAsync(MockNips.Iota);

        Assert.Equal(RiskLevel.High, report.Verdict);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(FindingCodes.VatNotActive, finding.Code);
        Assert.Contains("2026-02-10", finding.Description);
    }

    [Fact]
    public async Task Unknown_nip_has_no_verdict()
    {
        var report = await verifier.VerifyAsync(MockNips.Unknown);

        Assert.False(report.CompanyFound);
        Assert.Null(report.Verdict);
        Assert.Empty(report.Findings);
    }

    [Fact]
    public async Task Unavailable_crbr_is_reported_as_not_checked_never_as_ok()
    {
        var report = await TestEnvironment.CreateVerifier(MockRegistrySource.CrbrKey).VerifyAsync(MockNips.Alfa);

        Assert.True(report.IsIncomplete);
        Assert.Contains(report.Checks, c => c.Outcome == CheckOutcome.NotChecked && c.Source.StartsWith("CRBR"));
        Assert.DoesNotContain(report.Checks, c => c.Outcome == CheckOutcome.Passed && c.Source.StartsWith("CRBR"));
    }

    [Fact]
    public async Task Unavailable_white_list_means_company_could_not_be_checked()
    {
        var report = await TestEnvironment.CreateVerifier(MockRegistrySource.WhiteListKey).VerifyAsync(MockNips.Alfa);

        Assert.False(report.CompanyFound);
        Assert.Null(report.Verdict);
        Assert.Equal(CheckStatus.Unavailable, report.Profile.Vat.Status);
        Assert.Contains(report.Checks, c => c.Outcome == CheckOutcome.NotChecked);
    }

    [Fact]
    public async Task Unavailable_relations_hide_the_link_finding_but_mark_report_incomplete()
    {
        var report = await TestEnvironment.CreateVerifier(MockRegistrySource.RelationsKey).VerifyAsync(MockNips.Epsilon);

        Assert.Equal(RiskLevel.Low, report.Verdict);
        Assert.True(report.IsIncomplete);
    }

    [Fact]
    public async Task Every_fact_has_source_and_check_date()
    {
        var report = await verifier.VerifyAsync(MockNips.Delta);
        var checkpoint = new DateOnly(2026, 11, 2);

        Assert.All(report.Findings, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Source));
            Assert.Equal(checkpoint, f.CheckedOn);
            Assert.False(string.IsNullOrWhiteSpace(f.Advice));
        });
        Assert.All(report.Checks, c => Assert.Equal(checkpoint, c.CheckedOn));
    }

    [Fact]
    public void Mock_database_lists_all_eight_companies()
    {
        var companies = TestEnvironment.CreateMockSource().Companies;

        Assert.Equal(8, companies.Count);
        Assert.DoesNotContain(companies, c => c.Nip == MockNips.Unknown);
    }
}
