using System.Text;

namespace SimpleTGBot;

public static class Program
{
    // Метод main немного видоизменился для асинхронной работы
    public static async Task Main(string[] args)
    {
        // Православная кодировка
        Console.OutputEncoding = Encoding.UTF8;

        TelegramBot telegramBot = new TelegramBot();
        await telegramBot.Run();
    }
}
