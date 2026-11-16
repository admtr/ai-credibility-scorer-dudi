using System.Text;
using DuDi.Agent;
using DuDi.Cli;
using DuDi.Core.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Konfiguracja: appsettings.json → zmienne środowiskowe (np. Agent__Model=qwen3:8b) → argumenty (--Agent:Model=qwen3:8b).
// Katalog bazowy = katalog aplikacji, żeby appsettings.json i data/ znalazły się niezależnie od miejsca uruchomienia.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

var agentOptions = builder.Configuration.GetSection(AgentOptions.SectionName).Get<AgentOptions>() ?? new AgentOptions();
var dataOptions = builder.Configuration.GetSection(DataOptions.SectionName).Get<DataOptions>() ?? new DataOptions();

try
{
    builder.Services
        .AddDuDiCore()
        .AddDuDiSources(dataOptions);
}
catch (NotSupportedException ex)
{
    ConsoleUi.Error(ex.Message);
    return 1;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or FormatException)
{
    ConsoleUi.Error($"Nie udało się wczytać symulowanej bazy: {ex.Message}");
    return 1;
}

ConsoleUi.Accent("DuDi – asystent do wstępnej weryfikacji kontrahentów");
ConsoleUi.Dim($"Model: {agentOptions.Model} ({agentOptions.Provider}, {agentOptions.Endpoint}), temperatura {agentOptions.Temperature}, kontekst {agentOptions.ContextWindow} tokenów");
ConsoleUi.Dim($"Kontekst: {(agentOptions.ContextMode == ContextMode.Tools ? "tool calling – model sam wywołuje narzędzia" : "wstrzykiwany przez aplikację (bez narzędzi)")}");
ConsoleUi.Warning($"Dane: symulowana baza {ContextBuilder.TestDataMarker} – fikcyjne firmy, nie prawdziwe rejestry.");

var useTools = agentOptions.ContextMode == ContextMode.Tools;
var modelCheck = await ModelStartupCheck.RunAsync(agentOptions, requiresTools: useTools);

if (!modelCheck.Ready)
    return 1;

builder.Services.AddDuDiAgent(
    agentOptions,
    isTestData: dataOptions.Source == DataSourceKind.Mock,
    modelCheck.SupportsThinking);

using var host = builder.Build();

var demoCatalog = host.Services.GetService<IDemoCompanyCatalog>();

if (demoCatalog is { UnavailableSources.Count: > 0 })
    ConsoleUi.Warning($"Symulowana awaria źródeł: {string.Join(", ", demoCatalog.UnavailableSources)}");

await host.Services.GetRequiredService<ChatLoop>().RunAsync();
return 0;
