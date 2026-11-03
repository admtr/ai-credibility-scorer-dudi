using System.Text;
using DuDi.Agent;
using DuDi.Agent.Tools;
using DuDi.Cli;
using DuDi.Core.Registry;
using DuDi.Core.Verification;
using DuDi.Sources.Mock;
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

if (dataOptions.Source != DataSourceKind.Mock)
{
    ConsoleUi.Error("Na tym etapie dostępna jest tylko symulowana baza. Ustaw Data:Source=Mock (prawdziwe API: etapy 3–4).");
    return 1;
}

MockRegistrySource mockSource;

try
{
    mockSource = new MockRegistrySource(dataOptions.Mock);
}
catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or FormatException)
{
    ConsoleUi.Error($"Nie udało się wczytać symulowanej bazy: {ex.Message}");
    return 1;
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRegistrySource>(mockSource);
builder.Services.AddSingleton<CounterpartyVerifier>();
builder.Services.AddSingleton(new ContextBuilder(isTestData: true));
builder.Services.AddSingleton<RegistryTools>();

using var host = builder.Build();

ConsoleUi.Accent("DuDi – asystent do wstępnej weryfikacji kontrahentów");
ConsoleUi.Dim($"Model: {agentOptions.Model} ({agentOptions.Provider}, {agentOptions.Endpoint}), temperatura {agentOptions.Temperature}, kontekst {agentOptions.ContextWindow} tokenów");
ConsoleUi.Dim($"Kontekst: {(agentOptions.ContextMode == ContextMode.Tools ? "tool calling – model sam wywołuje narzędzia" : "wstrzykiwany przez aplikację (bez narzędzi)")}");
ConsoleUi.Warning($"Dane: symulowana baza {ContextBuilder.TestDataMarker} – fikcyjne firmy, nie prawdziwe rejestry.");

if (mockSource.UnavailableSources.Count > 0)
    ConsoleUi.Warning($"Symulowana awaria źródeł: {string.Join(", ", mockSource.UnavailableSources)}");

var useTools = agentOptions.ContextMode == ContextMode.Tools;
var modelCheck = await ModelStartupCheck.RunAsync(agentOptions, requiresTools: useTools);

if (!modelCheck.Ready)
    return 1;

var tools = host.Services.GetRequiredService<RegistryTools>();
var chatOptions = ChatClientFactory.CreateChatOptions(agentOptions, useTools ? tools.AsAITools() : null, modelCheck.SupportsThinking);

var contextInjector = useTools
    ? null
    : new ContextInjector(host.Services.GetRequiredService<CounterpartyVerifier>(), host.Services.GetRequiredService<ContextBuilder>());

using var chatClient = ChatClientFactory.Create(agentOptions);
var agent = new DuDiAgent(chatClient, chatOptions, SystemPrompt.Load(), contextInjector);

await new ChatLoop(agent, tools, mockSource).RunAsync();
return 0;
