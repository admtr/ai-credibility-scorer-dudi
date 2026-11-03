namespace DuDi.Agent;

public enum LlmProvider
{
    /// <summary>Lokalna Ollama.</summary>
    Ollama,

    /// <summary>Dowolne API zgodne z OpenAI, np. PCSS (https://llm.hpc.psnc.pl).</summary>
    OpenAICompatible
}

public enum ContextMode
{
    /// <summary>Model sam decyduje, co sprawdzić, i wywołuje narzędzia (tool calling).</summary>
    Tools,

    /// <summary>Aplikacja rozpoznaje NIP w wiadomości i sama dokleja dane kontrahenta do kontekstu (bez tool callingu).</summary>
    Injected
}

/// <summary>Sekcja <c>Agent</c> w appsettings.json. Zmiana modelu lub dostawcy nie wymaga zmian w kodzie.</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public LlmProvider Provider { get; set; } = LlmProvider.Ollama;

    public string Endpoint { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen3:4b";

    /// <summary>Niska temperatura: chcemy powtarzalnych, rzeczowych odpowiedzi.</summary>
    public float Temperature { get; set; } = 0.2f;

    /// <summary>
    /// Okno kontekstu (Ollama: <c>num_ctx</c>). Ustawiamy jawnie – przy domyślnym, małym oknie model po cichu
    /// „zapomina” dane kontrahenta z początku rozmowy.
    /// </summary>
    public int ContextWindow { get; set; } = 16384;

    /// <summary>Tryb „thinking” (Qwen3). Wyłączony – w czacie liczy się czas odpowiedzi.</summary>
    public bool Thinking { get; set; }

    public ContextMode ContextMode { get; set; } = ContextMode.Tools;

    /// <summary>Nazwa zmiennej środowiskowej z kluczem API (tylko dla <see cref="LlmProvider.OpenAICompatible"/>).</summary>
    public string ApiKeyEnvironmentVariable { get; set; } = "LLM_API_KEY";
}
