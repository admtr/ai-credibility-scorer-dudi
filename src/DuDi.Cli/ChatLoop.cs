using DuDi.Agent;
using DuDi.Agent.Tools;
using DuDi.Sources.Mock;

namespace DuDi.Cli;

/// <summary>Pętla czatu w konsoli: komendy, strumieniowanie odpowiedzi, przerwanie odpowiedzi przez Ctrl+C.</summary>
public sealed class ChatLoop(DuDiAgent agent, RegistryTools tools, MockRegistrySource? mockSource)
{
    private CancellationTokenSource? currentTurn;

    public async Task RunAsync()
    {
        tools.Invoked += call => ConsoleUi.Dim($"  [narzędzie] {call.Name}({call.Arguments})");

        // Ctrl+C przerywa bieżącą odpowiedź; poza odpowiedzią zamyka program jak zwykle.
        Console.CancelKeyPress += (_, e) =>
        {
            if (currentTurn is { } turn)
            {
                e.Cancel = true;
                turn.Cancel();
            }
        };

        PrintHelp();

        while (true)
        {
            Console.WriteLine();
            ConsoleUi.Write("Ty> ", ConsoleColor.Green);
            var input = Console.ReadLine();

            if (input is null)
                return;

            input = input.Trim();

            if (input.Length == 0)
                continue;

            if (input.StartsWith('/') || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                var (handled, message) = HandleCommand(input);

                if (!handled)
                    return;

                if (message is null)
                    continue;

                input = message;
            }

            await AnswerAsync(input);
        }
    }

    private async Task AnswerAsync(string message)
    {
        using var turn = new CancellationTokenSource();
        currentTurn = turn;

        Console.WriteLine();
        ConsoleUi.Write("DuDi> ", ConsoleColor.Cyan);

        try
        {
            await foreach (var chunk in agent.AskAsync(message, turn.Token))
                Console.Write(chunk);

            Console.WriteLine();

            if (mockSource is not null)
                ConsoleUi.Dim(ContextBuilder.TestDataMarker);
        }
        catch (OperationCanceledException) when (turn.IsCancellationRequested)
        {
            Console.WriteLine();
            ConsoleUi.Warning("Przerwano odpowiedź. Ta tura nie została zapisana w historii rozmowy.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine();
            ConsoleUi.Error($"Brak połączenia z modelem: {ex.Message}. Sprawdź, czy Ollama działa (ollama serve).");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            ConsoleUi.Error($"Błąd: {ex.Message}");
        }
        finally
        {
            currentTurn = null;
        }
    }

    /// <returns>handled=false kończy program; message to tekst do wysłania agentowi (np. z /nip).</returns>
    private (bool Handled, string? Message) HandleCommand(string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "/wyjdz" or "/wyjdź" or "/exit" or "exit":
                return (false, null);

            case "/pomoc" or "/help":
                PrintHelp();
                return (true, null);

            case "/reset":
                agent.Reset();
                ConsoleUi.Dim("Nowa rozmowa – historia wyczyszczona.");
                return (true, null);

            case "/firmy":
                PrintCompanies();
                return (true, null);

            case "/nip":
                if (argument.Length == 0)
                {
                    ConsoleUi.Warning("Użycie: /nip <NIP> [numer rachunku z faktury]");
                    return (true, null);
                }

                var nipAndAccount = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return (true, nipAndAccount.Length > 1
                    ? $"Sprawdź kontrahenta o NIP {nipAndAccount[0]}. Rachunek z faktury: {nipAndAccount[1]}."
                    : $"Sprawdź kontrahenta o NIP {nipAndAccount[0]}.");

            default:
                ConsoleUi.Warning($"Nieznana komenda {command}. Wpisz /pomoc.");
                return (true, null);
        }
    }

    private static void PrintHelp()
    {
        ConsoleUi.Dim("""
            Napisz pytanie albo podaj NIP kontrahenta, np. „Sprawdź firmę 948-812-73-53”.
            Komendy:
              /nip <NIP> [rachunek]  sprawdź kontrahenta (opcjonalnie z numerem rachunku z faktury)
              /firmy                 lista firm w symulowanej bazie
              /reset                 nowa rozmowa
              /pomoc                 ta pomoc
              /wyjdz                 koniec
            Ctrl+C przerywa bieżącą odpowiedź.
            """);
    }

    private void PrintCompanies()
    {
        if (mockSource is null)
        {
            ConsoleUi.Dim("Lista firm jest dostępna tylko w trybie Mock.");
            return;
        }

        ConsoleUi.Dim($"Firmy w symulowanej bazie {ContextBuilder.TestDataMarker}:");

        foreach (var (nip, name) in mockSource.Companies)
            ConsoleUi.Dim($"  {nip.ToDisplayString()}  {name}");

        ConsoleUi.Dim("  103-286-05-45  (NIP spoza bazy – do sprawdzenia reakcji na brak danych)");
    }
}
