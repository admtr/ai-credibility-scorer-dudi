using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace DuDi.Agent;

/// <summary>
/// Agent DuDi: instrukcje systemowe + historia rozmowy + model + (opcjonalnie) narzędzia.
/// Pętla tool callingu (model zgłasza wywołanie → aplikacja wykonuje → wynik wraca do modelu) jest obsługiwana
/// przez <c>UseFunctionInvocation</c> z Microsoft.Extensions.AI.
/// </summary>
public sealed class DuDiAgent
{
    private readonly IChatClient chatClient;
    private readonly ChatOptions chatOptions;
    private readonly string systemPrompt;
    private readonly ContextInjector? contextInjector;
    private readonly List<ChatMessage> history = [];

    /// <param name="contextInjector">Tylko dla <see cref="ContextMode.Injected"/>: aplikacja sama dokleja dane kontrahenta.</param>
    public DuDiAgent(IChatClient innerClient, ChatOptions chatOptions, string systemPrompt, ContextInjector? contextInjector = null)
    {
        chatClient = new ChatClientBuilder(innerClient)
            .UseFunctionInvocation()
            .Build();

        this.chatOptions = chatOptions;
        this.systemPrompt = systemPrompt;
        this.contextInjector = contextInjector;

        Reset();
    }

    /// <summary>Cała rozmowa, łącznie z wywołaniami narzędzi i ich wynikami.</summary>
    public IReadOnlyList<ChatMessage> History => history;

    /// <summary>Nowa rozmowa – zostają tylko instrukcje systemowe.</summary>
    public void Reset()
    {
        history.Clear();
        history.Add(new ChatMessage(ChatRole.System, systemPrompt));
    }

    /// <summary>Wysyła wiadomość i strumieniuje odpowiedź. Do historii trafia dopiero zakończona tura.</summary>
    public async IAsyncEnumerable<string> AskAsync(string userMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var content = contextInjector is null
            ? userMessage
            : await contextInjector.EnrichAsync(userMessage, cancellationToken);

        var message = new ChatMessage(ChatRole.User, content);
        List<ChatMessage> turn = [.. history, message];
        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in chatClient.GetStreamingResponseAsync(turn, chatOptions, cancellationToken))
        {
            updates.Add(update);

            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }

        // Przerwana lub nieudana tura nie zostawia w historii połowy rozmowy.
        history.Add(message);
        history.AddMessages(updates);
    }

    /// <summary>Jednorazowa odpowiedź bez strumieniowania – wygodna w ewaluacji.</summary>
    public async Task<string> AskAndWaitAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var text = new System.Text.StringBuilder();

        await foreach (var chunk in AskAsync(userMessage, cancellationToken))
            text.Append(chunk);

        return text.ToString();
    }
}
