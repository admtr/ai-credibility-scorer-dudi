using DuDi.Agent;
using OllamaSharp;
using OllamaSharp.Models;

namespace DuDi.Cli;

/// <param name="SupportsThinking">Czy model ma tryb „thinking” – tylko wtedy wysyłamy think=false.</param>
public sealed record ModelCheckResult(bool Ready, bool SupportsThinking);

/// <summary>
/// Sprawdza przed startem czatu, czy model jest gotowy: Ollama działa, model jest pobrany i obsługuje narzędzia.
/// Wzorowane na przykładach z laboratorium 04 (sulmar/shelly).
/// </summary>
public static class ModelStartupCheck
{
    public static async Task<ModelCheckResult> RunAsync(AgentOptions options, bool requiresTools)
    {
        if (options.Provider != LlmProvider.Ollama)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(options.ApiKeyEnvironmentVariable)))
            {
                ConsoleUi.Error($"Ustaw zmienną środowiskową {options.ApiKeyEnvironmentVariable} z kluczem API dla {options.Endpoint}.");
                return new ModelCheckResult(false, false);
            }

            return new ModelCheckResult(true, false);
        }

        using var ollama = new OllamaApiClient(new Uri(options.Endpoint), options.Model);

        if (!await IsRunningAsync(ollama))
        {
            ConsoleUi.Error($"Ollama nie działa pod adresem {options.Endpoint}. Uruchom ją poleceniem: ollama serve");
            return new ModelCheckResult(false, false);
        }

        ConsoleUi.Dim($"Ollama {await ollama.GetVersionAsync()}");

        var model = await GetModelInfoAsync(ollama, options.Model);

        if (model is null)
        {
            ConsoleUi.Warning($"Model {options.Model} nie jest pobrany.");

            var local = (await ollama.ListLocalModelsAsync()).Select(m => m.Name).ToList();
            ConsoleUi.Dim(local.Count == 0 ? "Brak pobranych modeli." : $"Pobrane modele: {string.Join(", ", local)}");

            Console.Write($"Pobrać {options.Model}? [t/N] ");

            if (!string.Equals(Console.ReadLine()?.Trim(), "t", StringComparison.OrdinalIgnoreCase))
                return new ModelCheckResult(false, false);

            if (!await PullAsync(ollama, options.Model))
                return new ModelCheckResult(false, false);

            model = await GetModelInfoAsync(ollama, options.Model);

            if (model is null)
            {
                ConsoleUi.Error($"Model {options.Model} nadal nie jest dostępny.");
                return new ModelCheckResult(false, false);
            }
        }

        // Capabilities pochodzą z /api/show, np. completion, tools, thinking. Bez "tools" tool calling nie zadziała.
        var capabilities = model.Capabilities ?? [];

        if (requiresTools && capabilities.Length > 0 && !capabilities.Contains("tools"))
        {
            ConsoleUi.Error($"Model {options.Model} nie obsługuje narzędzi (capabilities: {string.Join(", ", capabilities)}). " +
                            "Wybierz inny model albo ustaw Agent:ContextMode=Injected.");
            return new ModelCheckResult(false, false);
        }

        return new ModelCheckResult(true, capabilities.Contains("thinking"));
    }

    // IsRunningAsync rzuca HttpRequestException, gdy serwer nie nasłuchuje, zamiast zwrócić false.
    private static async Task<bool> IsRunningAsync(OllamaApiClient ollama)
    {
        try
        {
            return await ollama.IsRunningAsync();
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // /api/show zwraca 404 dla modelu, którego nie ma lokalnie.
    private static async Task<ShowModelResponse?> GetModelInfoAsync(OllamaApiClient ollama, string model)
    {
        try
        {
            return await ollama.ShowModelAsync(model);
        }
        catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static async Task<bool> PullAsync(OllamaApiClient ollama, string model)
    {
        try
        {
            await foreach (var progress in ollama.PullModelAsync(model))
            {
                if (progress is null)
                    continue;

                var percent = progress.Total > 0 ? $" {100.0 * progress.Completed / progress.Total:0}%" : "";
                Console.Write($"\r{progress.Status}{percent}".PadRight(60));
            }

            Console.WriteLine();
            return true;
        }
        catch (Exception e) when (e is HttpRequestException or OllamaSharp.Models.Exceptions.ResponseError)
        {
            Console.WriteLine();
            ConsoleUi.Error($"Nie udało się pobrać modelu {model}: {e.Message}");
            return false;
        }
    }
}
