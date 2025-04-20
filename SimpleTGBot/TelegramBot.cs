using SimpleTGBot.Logging;
using SimpleTGBot.MemeGen;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleTGBot;

internal class TelegramBot
{
    private string token;
    private Logger logger;
    private Dictionary<long, DialogData> dialogs;
    private TempStorage temp;
    private HttpClient httpClient;

    public TelegramBot(string token, Logger logger)
    {
        this.token = token;
        this.logger = logger;
        dialogs = new Dictionary<long, DialogData>();
        temp = new TempStorage();
        httpClient = new HttpClient();
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
            logger.Info($"Бот @{me.Username} запущен.\r\nДля остановки нажмите клавишу Esc...");
        }
        catch (ApiRequestException)
        {
            logger.Fatal("Указан неправильный токен");
            goto botQuit;
        }
        
        while (Console.ReadKey().Key != ConsoleKey.Escape){}

    botQuit:
        cts.Cancel();
        temp.Dispose();
    }
    
    /// <summary>
    /// Обработчик события получения сообщения.
    /// </summary>
    /// <param name="botClient">Клиент, который получил сообщение</param>
    /// <param name="update">Событие, произошедшее в чате. Новое сообщение, голос в опросе, исключение из чата и т. д.</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    async Task OnMessageReceived(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Chat.Type != ChatType.Private) return;

        DialogData dialogData;
        if (!dialogs.ContainsKey(message.Chat.Id))
        {
            dialogData = new DialogData() { state = DialogState.Initial };
            dialogs[message.Chat.Id] = dialogData;
        } else
        {
            dialogData = dialogs[message.Chat.Id];
        }

        switch (dialogData.state)
        {
            case DialogState.Initial:
                {
                    bool replied = false;
                    if (message.Photo is { } picture)
                    {
                        replied = true;
                        await DialogHandleDemotivatorPicture(botClient, dialogData, message, picture, cancellationToken);
                    }
                    else if (message.Text is { } messageText)
                    {
                        if (Interactions.IsStartCommand(messageText) || Interactions.IsHello(messageText))
                        {
                            _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                            dialogData.state = DialogState.AwaitingPicture;
                            replied = true;
                        }
                    }
                    if (!replied)
                    {
                        _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.sayHelloMessage, replyMarkup: Interactions.initialReplyMarkup);
                    }
                    break;
                }
            case DialogState.AwaitingPicture:
                {
                    if (message.Photo is { } picture)
                    {
                        await DialogHandleDemotivatorPicture(botClient, dialogData, message, picture, cancellationToken);
                    }
                    else
                    {
                        bool reacted = false;
                        if (message.Text is { } messageText)
                        {
                            if (Interactions.IsCancellation(messageText))
                            {
                                dialogData.state = DialogState.Initial;
                                _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.quickActionReplyMarkup);
                                reacted = true;
                            }
                        }
                        if (!reacted)
                            _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.sendPictureOrQuitMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                    }
                    break;
                }
        }
    }

    async Task DialogHandleDemotivatorPicture(ITelegramBotClient botClient, DialogData dialogData, Message message, PhotoSize[] picture, CancellationToken cancellationToken)
    {
        string largestSizeId = picture[picture.Length - 1].FileId;
        Telegram.Bot.Types.File pictureFile = await botClient.GetFileAsync(largestSizeId, cancellationToken);
        string pictureExtension = pictureFile.FilePath.Substring(pictureFile.FilePath.LastIndexOf('.') + 1);
        try
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(FilePathToUrl(pictureFile.FilePath), cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                (string tempFileName, FileStream tempFile) = temp.newTemporaryFile("pic", pictureExtension);
                await response.Content.CopyToAsync(tempFile);
                tempFile.Close();
                logger.Info($"Файл картинки {tempFileName} загружен от пользователя {message.From.FirstName}[{message.From.Id}]");
                dialogData.inputPictureFilename = tempFileName;
            }
        }
        catch (Exception e)
        {
            logger.Error("Ошибка при скачивании картинки от пользователя: " + e.GetType().Name + ": " + e.Message);
            logger.Error(e.StackTrace ?? "");
            _ = botClient.SendTextMessageAsync(message.Chat.Id, "Ошибка :(");
            dialogData.state = DialogState.Initial;
        }
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

        logger.Error(errorMessage);
        
        return Task.CompletedTask;
    }

    private string FilePathToUrl(string filePath)
    {
        return $"https://api.telegram.org/file/bot{token}/{filePath}";
    }
}
