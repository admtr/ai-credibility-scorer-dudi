using System.ClientModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using OpenAI;

namespace DuDi.Agent;

/// <summary>Tworzy <see cref="IChatClient"/> dla wybranego dostawcy. Reszta aplikacji zna tylko abstrakcję.</summary>
public static class ChatClientFactory
{
    public static IChatClient Create(AgentOptions options) => options.Provider switch
    {
        LlmProvider.Ollama => new OllamaApiClient(new Uri(options.Endpoint), options.Model),
        LlmProvider.OpenAICompatible => CreateOpenAICompatible(options),
        _ => throw new NotSupportedException($"Nieobsługiwany dostawca modelu: {options.Provider}.")
    };

    /// <param name="modelSupportsThinking">Czy model ma tryb „thinking” (z /api/show w Ollamie). Tylko wtedy wysyłamy think=false.</param>
    public static ChatOptions CreateChatOptions(AgentOptions options, IList<AITool>? tools, bool modelSupportsThinking)
    {
        var chatOptions = new ChatOptions
        {
            Temperature = options.Temperature,
            Tools = tools
        };

        if (options.Provider == LlmProvider.Ollama)
        {
            chatOptions.AddOllamaOption(OllamaOption.NumCtx, options.ContextWindow);

            // OllamaSharp mapuje ReasoningEffort.None na "think": false w zapytaniu do Ollamy.
            if (modelSupportsThinking && !options.Thinking)
                chatOptions.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None };
        }

        return chatOptions;
    }

    private static IChatClient CreateOpenAICompatible(AgentOptions options)
    {
        // Klucz czytamy ze zmiennej środowiskowej, żeby nie trafił do repozytorium ani historii poleceń.
        var apiKey = Environment.GetEnvironmentVariable(options.ApiKeyEnvironmentVariable);

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException($"Ustaw zmienną środowiskową {options.ApiKeyEnvironmentVariable} z kluczem API dla {options.Endpoint}.");

        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) })
            .GetChatClient(options.Model)
            .AsIChatClient();
    }
}
