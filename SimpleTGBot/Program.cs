using System.Text;
using Microsoft.Data.Sqlite;
using SimpleTGBot.Logging;

namespace SimpleTGBot;

public static class Program
{
    // Метод main немного видоизменился для асинхронной работы
    public static async Task<int> Main(string[] args)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version < new Version(6, 1))
        {
            Console.WriteLine("К сожалению, из-за используемых графических функций бот поддерживает только Windows начиная с версии 7.");
            return 8;
        }
        // Православная кодировка
        Console.OutputEncoding = Encoding.UTF8;

        using SqliteConnection db = new("Data Source=" + Config.DEFAULT_DATABASE_FILENAME);
        db.Open();

        PrepareDatabaseTables(db);

        string? botToken = Config.TryReadBotTokenFile();

        if (botToken == null)
        {
            Console.WriteLine($"Файл {Config.DEFAULT_BOT_TOKEN_FILENAME} не найден. Вы можете указать токен вручную:");
            Console.Write("$ ");
            botToken = Console.ReadLine();
        }

        if (botToken == null)
        {
            return 1;
        }

        using (Logger logger = new Logger())
        {
            logger.Sinks.Add(new StdoutSink());
            TelegramBot telegramBot = new TelegramBot(botToken, logger, db);
            await telegramBot.Run();
        }

        db.Close();

        return 0;
    }
    
    static void PrepareDatabaseTables(SqliteConnection db)
    {
        var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, selected_preset INTEGER)";
        cmd.ExecuteNonQuery();

        cmd = db.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS user_presets (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER,
            name TEXT,
            outline_color INTEGER,
            title_color INTEGER,
            subtitle_color INTEGER)";
        cmd.ExecuteNonQuery();
    }
}
