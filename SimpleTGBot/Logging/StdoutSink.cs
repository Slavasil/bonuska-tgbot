namespace SimpleTGBot.Logging;

internal class StdoutSink : ILogSink
{
    private readonly ConsoleColor[] colors = [ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.DarkRed, ConsoleColor.Red];
    private ConsoleColor originalConsoleColor;

    public StdoutSink()
    {
        originalConsoleColor = Console.ForegroundColor;
    }

    public void Log(DateTime time, LogLevel level, string message)
    {
        Console.ForegroundColor = colors[(int)level];
        foreach (string line in message.Split(Environment.NewLine))
            Console.WriteLine($"({time:u}) [{level.GetName()}] {line}");
    }

    public void Dispose() {
        Console.ForegroundColor = originalConsoleColor;
    }
}
