using SimpleTGBot.MemeGen;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleTGBot;

public class TelegramBot
{
    private string token;

    public TelegramBot(string token)
    {
        this.token = token;
    }
    
    /// <summary>
    /// Инициализирует и обеспечивает работу бота до нажатия клавиши Esc
    /// </summary>
    public async Task Run()
    {
        var botClient = new TelegramBotClient(token);

        using CancellationTokenSource cts = new CancellationTokenSource();
        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new [] { UpdateType.Message }
        };
        botClient.StartReceiving(
            updateHandler: OnMessageReceived,
            pollingErrorHandler: OnErrorOccured,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        try
        {
            var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
            Console.WriteLine($"Бот @{me.Username} запущен.\nДля остановки нажмите клавишу Esc...");
        }
        catch (ApiRequestException)
        {
            Console.WriteLine("Указан неправильный токен");
            goto botQuit;
        }
        
        while (Console.ReadKey().Key != ConsoleKey.Escape){}

botQuit:
        cts.Cancel();
    }
    
    /// <summary>
    /// Обработчик события получения сообщения.
    /// </summary>
    /// <param name="botClient">Клиент, который получил сообщение</param>
    /// <param name="update">Событие, произошедшее в чате. Новое сообщение, голос в опросе, исключение из чата и т. д.</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    async Task OnMessageReceived(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message is null)
        {
            return;
        }
        if (message.Text is not { } messageText)
        {
            return;
        }
        var chatId = message.Chat.Id;

        Console.WriteLine($"Получено сообщение в чате {chatId}: '{messageText}'");

        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Ты написал:\n" + messageText,
            cancellationToken: cancellationToken);

        // грязный тест
        MemoryStream demotivatorPng = DemotivatorGen.MakePictureDemotivator("pic.png", [new DemotivatorText() {Title=messageText, Subtitle=messageText}], DemotivatorGen.DefaultStyle());
        await botClient.SendPhotoAsync(message.Chat.Id, new InputFile(demotivatorPng, "dem.png"));
    }

    /// <summary>
    /// Обработчик исключений, возникших при работе бота
    /// </summary>
    /// <param name="botClient">Клиент, для которого возникло исключение</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    /// <returns></returns>
    Task OnErrorOccured(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        
        return Task.CompletedTask;
    }
}
