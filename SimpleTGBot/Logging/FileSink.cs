namespace SimpleTGBot.Logging;

internal class FileSink : ILogSink
{
    StreamWriter file;

    public FileSink(string filename)
    {
        file = new StreamWriter(filename, true);
    }

    public void Log(DateTime time, LogLevel level, string message)
    {
        foreach (string line in message.Split(Environment.NewLine))
            file.WriteLine($"({time:u}) [{level.GetName()}] {line}");
    }

    public void Dispose()
    {
        file.Dispose();
    }
}
