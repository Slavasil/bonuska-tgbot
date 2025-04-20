using System.IO;

namespace SimpleTGBot;

internal class Config
{
    public const string DEFAULT_BOT_TOKEN_FILENAME = "telegram_token.txt";
    public const string DEFAULT_DATABASE_FILENAME = "demotivatorbot.db";
    
    public static string? TryReadBotTokenFile()
    {
        try
        {
            return File.ReadAllText(DEFAULT_BOT_TOKEN_FILENAME).Trim();
        } catch (FileNotFoundException)
        {
            return null;
        }
    }
}
