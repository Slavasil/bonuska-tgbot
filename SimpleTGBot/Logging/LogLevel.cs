namespace SimpleTGBot.Logging;

internal enum LogLevel : int
{
    Debug = 0, Info, Warning, Error, Fatal
}
static class LogLevelExt
{
    public static string GetName(this LogLevel level)
    {
        return level switch {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Fatal => "FATAL"
        };
    }
}
