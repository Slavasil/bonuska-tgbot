using System.Text;

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

        TelegramBot telegramBot = new TelegramBot(botToken);
        await telegramBot.Run();

        return 0;
    }
}
