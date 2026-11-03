using DuDi.Agent;
using DuDi.Agent.Tools;
using DuDi.Sources.Mock;
using Microsoft.Extensions.AI;

namespace DuDi.Tests;

public class ContextBuilderTests
{
    private readonly ContextBuilder builder = new(isTestData: true);

    [Fact]
    public async Task Context_contains_verdict_findings_advice_and_test_marker()
    {
        var report = await TestEnvironment.CreateVerifier().VerifyAsync(MockNips.Beta, MockNips.BetaInvoiceAccountNotOnList);

        var context = builder.Build(report);

        Assert.StartsWith("<dane_kontrahenta>", context);
        Assert.Contains(ContextBuilder.TestDataMarker, context);
        Assert.Contains("ryzyko WYSOKIE", context);
        Assert.Contains("nie zmieniaj go", context);
        Assert.Contains("Zalecenie:", context);
        Assert.Contains("ZAW-NR", context);
        Assert.Contains("Jan Kowalski", context);
        Assert.Contains("(Biała lista VAT (MF), 2026-11-02)", context);
    }

    [Fact]
    public async Task Context_for_unknown_nip_forbids_inventing_data()
    {
        var report = await TestEnvironment.CreateVerifier().VerifyAsync(MockNips.Unknown);

        var context = builder.Build(report);

        Assert.Contains("Nie znaleziono podmiotu", context);
        Assert.Contains("Nie podawaj żadnych informacji", context);
        Assert.DoesNotContain("WERDYKT", context);
    }

    [Fact]
    public async Task Context_marks_unavailable_source_as_not_checked()
    {
        var report = await TestEnvironment.CreateVerifier(MockRegistrySource.SanctionsKey).VerifyAsync(MockNips.Alfa);

        var context = builder.Build(report);

        Assert.Contains("[NIE SPRAWDZONO] Lista sankcyjna UE", context);
        Assert.Contains("niepełnych danych", context);
    }

    [Fact]
    public void System_prompt_is_embedded_and_names_all_tools()
    {
        var prompt = SystemPrompt.Load();

        Assert.Contains("DuDi", prompt);
        Assert.Contains(RegistryTools.CheckCounterpartyName, prompt);
        Assert.Contains(RegistryTools.CheckAccountName, prompt);
        Assert.Contains(RegistryTools.FindPersonLinksName, prompt);
    }
}

public class RegistryToolsTests
{
    private static RegistryTools CreateTools(params string[] unavailableSources)
    {
        var source = TestEnvironment.CreateMockSource(unavailableSources);
        var verifier = new Core.Verification.CounterpartyVerifier(source, FixedTimeProvider.Checkpoint);
        return new RegistryTools(verifier, source, new ContextBuilder(isTestData: true), FixedTimeProvider.Checkpoint);
    }

    [Fact]
    public async Task Invalid_nip_returns_error_text_instead_of_throwing()
    {
        var result = await CreateTools().CheckCounterpartyAsync("1234567890");

        Assert.StartsWith("BŁĄD:", result);
        Assert.Contains("sumę kontrolną", result);
    }

    [Fact]
    public async Task Invalid_account_is_reported_but_company_is_still_checked()
    {
        var result = await CreateTools().CheckCounterpartyAsync("9255757334", "123");

        Assert.StartsWith("UWAGA:", result);
        Assert.Contains("<dane_kontrahenta>", result);
    }

    [Fact]
    public async Task Tool_invocation_is_reported_to_listeners()
    {
        var tools = CreateTools();
        var calls = new List<ToolInvocation>();
        tools.Invoked += calls.Add;

        await tools.CheckCounterpartyAsync("948-812-73-53");

        Assert.Equal(RegistryTools.CheckCounterpartyName, Assert.Single(calls).Name);
    }

    [Theory]
    [InlineData("70105014453872590736196366", "JEST przypisany")]
    [InlineData("20124010379198662181871482", "NIE JEST przypisany")]
    public async Task Account_check_reports_white_list_result(string account, string expected)
    {
        var result = await CreateTools().CheckAccountAsync("9255757334", account);

        Assert.Contains(expected, result);
        Assert.Contains(ContextBuilder.TestDataMarker, result);
    }

    [Fact]
    public async Task Account_check_with_unavailable_white_list_does_not_confirm_anything()
    {
        var result = await CreateTools(MockRegistrySource.WhiteListKey).CheckAccountAsync("9255757334", "70105014453872590736196366");

        Assert.Contains("NIE SPRAWDZONO", result);
        Assert.DoesNotContain("JEST przypisany", result);
    }

    [Fact]
    public async Task Person_links_list_all_companies_of_board_member()
    {
        var result = await CreateTools().FindPersonLinksAsync("Tomasz Wójcik");

        Assert.Contains("DELTA LOGISTICS", result);
        Assert.Contains("EPSILON CONSULTING", result);
        Assert.Contains("265-017-23-91", result);
    }

    [Fact]
    public async Task Person_links_require_first_and_last_name()
    {
        Assert.StartsWith("BŁĄD:", await CreateTools().FindPersonLinksAsync("Wójcik"));
    }

    [Fact]
    public void Tools_are_exposed_with_names_and_parameter_schema()
    {
        var functions = CreateTools().AsAITools().OfType<AIFunction>().ToList();

        Assert.Equal(
            new[] { RegistryTools.CheckCounterpartyName, RegistryTools.CheckAccountName, RegistryTools.FindPersonLinksName },
            functions.Select(f => f.Name).ToArray());

        var schema = functions[0].JsonSchema.GetRawText();
        Assert.Contains("\"nip\"", schema);
        Assert.Contains("numerRachunku", schema);
        Assert.DoesNotContain("cancellationToken", schema);
    }

    [Fact]
    public async Task Tool_can_be_invoked_the_way_the_model_calls_it()
    {
        // Ta sama ścieżka co przy tool callingu: argumenty z JSON-a modelu → AIFunction → metoda C#.
        var function = CreateTools().AsAITools().OfType<AIFunction>().First();

        var result = await function.InvokeAsync(new AIFunctionArguments { ["nip"] = "8097202819" });

        Assert.Contains("ryzyko KRYTYCZNE", result?.ToString());
    }
}
