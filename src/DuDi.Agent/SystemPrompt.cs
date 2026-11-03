namespace DuDi.Agent;

/// <summary>Instrukcje systemowe agenta. Trzymamy je w osobnym pliku Markdown, żeby łatwo było je czytać i wersjonować.</summary>
public static class SystemPrompt
{
    private const string ResourceName = "DuDi.Agent.Prompts.system-prompt.md";

    public static string Load()
    {
        using var stream = typeof(SystemPrompt).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Brak zasobu {ResourceName} w assembly DuDi.Agent.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
