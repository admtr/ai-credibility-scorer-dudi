namespace DuDi.Cli;

/// <summary>Kolorowe komunikaty w konsoli.</summary>
public static class ConsoleUi
{
    public static void Error(string text) => WriteLine(text, ConsoleColor.Red);

    public static void Warning(string text) => WriteLine(text, ConsoleColor.Yellow);

    public static void Dim(string text) => WriteLine(text, ConsoleColor.DarkGray);

    public static void Accent(string text) => WriteLine(text, ConsoleColor.Cyan);

    public static void Write(string text, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }

    public static void WriteLine(string text, ConsoleColor color) => Write(text + Environment.NewLine, color);
}
