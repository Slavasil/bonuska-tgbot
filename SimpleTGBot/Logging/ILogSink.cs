namespace SimpleTGBot.Logging;

internal interface ILogSink : IDisposable
{
    public void Log(DateTime time, LogLevel level, string message);
}
