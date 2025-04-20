namespace SimpleTGBot.Logging;

internal class Logger : IDisposable
{
    public List<ILogSink> Sinks;

    public Logger(params ILogSink[] sinks)
    {
        Sinks = new List<ILogSink>(sinks);
    }

    public void Log(LogLevel level, string message)
    {
        DateTime now = DateTime.Now;
        foreach (var sink in Sinks)
        {
            sink.Log(now, level, message);
        }
    }

    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message) => Log(LogLevel.Warning, message);
    public void Error(string message) => Log(LogLevel.Error, message);
    public void Fatal(string message) => Log(LogLevel.Fatal, message);

    public void Dispose()
    {
        foreach (var sink in Sinks)
        {
            sink.Dispose();
        }
    }
}
