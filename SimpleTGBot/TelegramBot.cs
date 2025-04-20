using System.Drawing;
using Microsoft.Data.Sqlite;
using SimpleTGBot.Logging;
using SimpleTGBot.MemeGen;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTGBot;

internal class TelegramBot
{
    private string token;
    private Logger logger;
    private SqliteConnection database;
    private Dictionary<long, DialogData> dialogs;
    private TempStorage temp;
    private HttpClient httpClient;

    public TelegramBot(string token, Logger logger, SqliteConnection db)
    {
        this.token = token;
        this.logger = logger;
        dialogs = new Dictionary<long, DialogData>();
        temp = new TempStorage();
        httpClient = new HttpClient();
        database = db;
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
        if (message.From is not { } user) return;

        DialogData dialogData;
        if (!dialogs.ContainsKey(message.Chat.Id))
        {
            dialogData = new DialogData() { state = DialogState.Initial };
            dialogs[message.Chat.Id] = dialogData;
            if (!await IsUserInDatabase(user))
            {
                await AddUserToDatabase(user);
            }
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
                        if (Interactions.IsStartCommand(messageText))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.greetingMessage, replyMarkup: Interactions.mainReplyMarkup);
                            replied = true;
                        }
                        else if (Interactions.IsHello(messageText))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                            dialogData.state = DialogState.AwaitingPicture;
                            replied = true;
                        }
                        else if (Interactions.IsGotoSettings(messageText))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.settingsMessage, replyMarkup: Interactions.settingsReplyMarkup);
                            dialogData.state = DialogState.Settings;
                            replied = true;
                        }
                    }
                    if (!replied)
                    {
                        _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.sayHelloMessage, replyMarkup: Interactions.mainReplyMarkup);
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
                                _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
                                reacted = true;
                            }
                        }
                        if (!reacted)
                            _ = botClient.SendTextMessageAsync(message.Chat.Id, Interactions.sendPictureOrQuitMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                    }
                    break;
                }
            case DialogState.AwaitingTitle:
                {
                    if (message.Text is { } messageText)
                    {
                        if (!Interactions.IsCancelButton(messageText))
                        {
                            await DialogHandleDemotivatorText(botClient, dialogData, message, messageText, cancellationToken);
                        } else
                        {
                            DialogCancelDemotivatorCreation(dialogData);
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
                        }
                    } else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.sendTitleOrQuitMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                    }
                    break;
                }
            case DialogState.AwaitingSubtitle:
                {
                    if (message.Text is { } messageText)
                    {
                        if (!Interactions.IsCancelButton(messageText))
                        {
                            await DialogHandleDemotivatorSubtitle(botClient, dialogData, message, messageText, cancellationToken);
                        }
                        else
                        {
                            DialogCancelDemotivatorCreation(dialogData);
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
                        }
                    }
                    break;
                }
            case DialogState.ShowingResult:
                {
                    bool replied = false;
                    if (message.Text is { } messageText)
                    {
                        if (messageText == Interactions.doneButtonText)
                        {
                            replied = true;
                            DialogFinishDemotivatorCreation(dialogData);
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
                        }
                    }
                    if (!replied)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.chooseActionMessage, replyMarkup: Interactions.resultActionReplyMarkup);
                    }
                    break;
                }
            case DialogState.Settings:
                {
                    bool replied = false;
                    if (message.Text is { } messageText)
                    {
                        if (messageText == Interactions.gotoPresetsButtonText)
                        {
                            replied = true;
                            await DialogShowPresets(botClient, user, message.Chat.Id, dialogData);
                        }
                        else if (messageText == Interactions.backButtonText)
                        {
                            replied = true;
                            dialogData.state = DialogState.Initial;
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
                        }
                    }
                    if (!replied)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.chooseActionMessage, replyMarkup: Interactions.settingsReplyMarkup);
                    }
                    break;
                }
            case DialogState.ViewingPresets:
                {
                    bool replied = false;
                    if (message.Text is { } messageText)
                    {
                        if (messageText == Interactions.backButtonText)
                        {
                            replied = true;
                            dialogData.state = DialogState.Settings;
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.settingsMessage, replyMarkup: Interactions.settingsReplyMarkup);
                        }
                        else if (messageText == Interactions.choosePresetButtonText)
                        {
                            replied = true;
                            dialogData.state = DialogState.ChoosingPreset;
                            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.choosePresetMessage, replyMarkup: Interactions.backButtonReplyMarkup);
                        }
                    }
                    if (!replied)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.chooseActionMessage, replyMarkup: Interactions.presetsReplyMarkup);
                    }
                    break;
                }
            case DialogState.ChoosingPreset:
                {
                    bool replied = false;
                    if (message.Text is { } messageText)
                    {
                        if (messageText == Interactions.backButtonText)
                        {
                            replied = true;
                            await DialogShowPresets(botClient, user, message.Chat.Id, dialogData);
                        }
                        else if (int.TryParse(messageText, out int presetIndex))
                        {
                            if (presetIndex >= 1 && presetIndex <= dialogData.shownPresets.Count())
                            {
                                replied = true;
                                presetIndex -= 1;
                                long id = dialogData.shownPresets[presetIndex].Id;
                                await SetActiveUserPreset(user, id);
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Выбран пресет \"" + dialogData.shownPresets[presetIndex].Name + "\"", cancellationToken: cancellationToken);
                                await DialogShowPresets(botClient, user, message.Chat.Id, dialogData);
                            }
                        }
                    }
                    if (!replied)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.enterPresetNumberMessage, replyMarkup: Interactions.presetsReplyMarkup);
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
                tempFile.Dispose();
                logger.Info($"Файл картинки {tempFileName} загружен от пользователя {message.From.FirstName}[{message.From.Id}]");
                dialogData.inputPictureFilename = tempFileName;
                dialogData.state = DialogState.AwaitingTitle;
                await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingTitleMessage, replyMarkup: Interactions.backButtonReplyMarkup);
            }
        }
        catch (Exception e)
        {
            logger.Error("Ошибка при скачивании картинки от пользователя: " + e.GetType().Name + ": " + e.Message);
            logger.Error(e.StackTrace ?? "");
            dialogData.state = DialogState.Initial;
            await botClient.SendTextMessageAsync(message.Chat.Id, "Ошибка :(");
            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingPictureMessage, replyMarkup: Interactions.mainReplyMarkup);
        }
    }

    async Task DialogHandleDemotivatorText(ITelegramBotClient botClient, DialogData dialogData, Message message, string text, CancellationToken cancellationToken)
    {
        string[] lines = text.Split('\n');
        string title;
        string? subtitle = null;
        if (lines.Length > 1)
        {
            title = lines[0];
            subtitle = string.Join(' ', lines.Skip(1));
        } else
        {
            title = lines.First();
        }

        if (subtitle != null)
        {
            logger.Info($"Генерирую простой демотиватор: [\"{title}\", \"{subtitle}\"]");
            MemoryStream demotivator = DemotivatorGen.MakePictureDemotivator(
                dialogData.inputPictureFilename!,
                [new DemotivatorText() { Title = title, Subtitle = subtitle }],
                DemotivatorGen.DefaultStyle());
            dialogData.state = DialogState.ShowingResult;
            await botClient.SendPhotoAsync(message.Chat.Id, new InputFile(demotivator, "dem.png"), caption: Interactions.showingResultMessage, replyMarkup: Interactions.resultActionReplyMarkup, cancellationToken: cancellationToken);
            demotivator.Dispose();
        } else
        {
            logger.Info($"Пользователь ввёл заголовок: \"{title}\"");
            dialogData.inputTitle = title;
            dialogData.state = DialogState.AwaitingSubtitle;
            await botClient.SendTextMessageAsync(message.Chat.Id, Interactions.awaitingSubtitleMessage, replyMarkup: Interactions.backButtonReplyMarkup, cancellationToken: cancellationToken);
        }
    }

    async Task DialogHandleDemotivatorSubtitle(ITelegramBotClient botClient, DialogData dialogData, Message message, string subtitle, CancellationToken cancellationToken)
    {
        subtitle = subtitle != "." ? subtitle.Replace('\n', ' ') : "";
        string title = dialogData.inputTitle!;

        logger.Info($"Генерирую простой демотиватор: [\"{title}\", \"{subtitle}\"]");
        MemoryStream demotivator = DemotivatorGen.MakePictureDemotivator(
                dialogData.inputPictureFilename!,
                [new DemotivatorText() { Title = title, Subtitle = subtitle }],
                DemotivatorGen.DefaultStyle());
        dialogData.state = DialogState.ShowingResult;
        await botClient.SendPhotoAsync(message.Chat.Id, new InputFile(demotivator, "dem.png"), caption: Interactions.showingResultMessage, replyMarkup: Interactions.resultActionReplyMarkup, cancellationToken: cancellationToken);
        demotivator.Dispose();
    }

    void DialogCancelDemotivatorCreation(DialogData dialogData)
    {
        dialogData.state = DialogState.Initial;
        if (dialogData.inputPictureFilename != null)
            temp.deleteTemporaryFile(dialogData.inputPictureFilename);
    }

    void DialogFinishDemotivatorCreation(DialogData dialogData)
    {
        dialogData.state = DialogState.Initial;
        if (dialogData.inputPictureFilename != null)
            temp.deleteTemporaryFile(dialogData.inputPictureFilename);
    }

    async Task DialogShowPresets(ITelegramBotClient botClient, User user, long chatId, DialogData dialogData)
    {
        dialogData.state = DialogState.ViewingPresets;
        (UserPreset[] presets, long activePresetId) = await GetUserPresets(user);
        int activePresetIndex = 0;
        for (int i = 0; i < presets.Length; ++i)
        {
            if (presets[i].Id == activePresetId)
            {
                activePresetIndex = i;
                break;
            }
        }
        dialogData.shownPresets = presets;
        await botClient.SendTextMessageAsync(chatId, Interactions.MakePresetListMessage(presets.Select(preset => preset.Name).ToArray(), activePresetIndex), replyMarkup: Interactions.presetsReplyMarkup);
    }

    async Task AddUserToDatabase(User u)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "INSERT INTO users (id) VALUES ($1)";
        cmd.Parameters.AddWithValue("$1", u.Id);
        await cmd.ExecuteNonQueryAsync();

        cmd = database.CreateCommand();
        UserPreset defaultPreset = UserPreset.Default();
        defaultPreset.OwnerId = u.Id;
        long defaultPresetId = await AddPresetToDatabase(defaultPreset);

        cmd = database.CreateCommand();
        cmd.CommandText = "UPDATE users SET selected_preset = $1 WHERE id = $2";
        cmd.Parameters.AddWithValue("$1", defaultPresetId);
        cmd.Parameters.AddWithValue("$2", u.Id);
        await cmd.ExecuteNonQueryAsync();
    }

    async Task<long> AddPresetToDatabase(UserPreset p)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO user_presets (user_id, name, outline_color, title_color, subtitle_color) VALUES ($1, $2, $3, $4, $5);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$1", p.OwnerId);
        cmd.Parameters.AddWithValue("$2", p.Name);
        cmd.Parameters.AddWithValue("$3", (long)p.OutlineColor.ToArgb() & 0xFFFFFFL);
        cmd.Parameters.AddWithValue("$4", (long)p.TitleColor.ToArgb() & 0xFFFFFFL);
        cmd.Parameters.AddWithValue("$5", (long)p.SubtitleColor.ToArgb() & 0xFFFFFFL);
        return (long)await cmd.ExecuteScalarAsync();
    }

    async Task<bool> IsUserInDatabase(User u)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "SELECT id FROM users WHERE id = $1";
        cmd.Parameters.AddWithValue("$1", u.Id);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    async Task<(UserPreset[], long)> GetUserPresets(User u)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "SELECT id, user_id, name, outline_color, title_color, subtitle_color FROM user_presets WHERE user_id = $1";
        cmd.Parameters.AddWithValue("$1", u.Id);
        using var reader = await cmd.ExecuteReaderAsync();
        List<UserPreset> result = new List<UserPreset>();
        while (reader.Read())
        {
            result.Add(new UserPreset()
            {
                Id = reader.GetInt64(0),
                OwnerId = reader.GetInt64(1),
                Name = reader.GetString(2),
                OutlineColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(3))),
                TitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(4))),
                SubtitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(5))),
            });
        }
        return (result.ToArray(), (await GetActiveUserPreset(u)).Id);
    }

    async Task<UserPreset?> GetUserPresetByName(User u, string name)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "SELECT id, outline_color, title_color, subtitle_color FROM user_presets WHERE name = $1";
        cmd.Parameters.AddWithValue("$1", name);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.Read())
            {
                return new UserPreset()
                {
                    Id = reader.GetInt64(0),
                    OwnerId = u.Id,
                    Name = name,
                    OutlineColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(1))),
                    TitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(2))),
                    SubtitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(3))),
                };
            }
            else
            {
                return null;
            }
        }
    }

    async Task DeleteUserPreset(long id)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "DELETE FROM user_presets WHERE id = $1";
        cmd.Parameters.AddWithValue("$1", id);
        cmd.ExecuteNonQuery();
    }

    async Task<UserPreset> GetActiveUserPreset(User u)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "SELECT selected_preset FROM users WHERE id = $1";
        cmd.Parameters.AddWithValue("$1", u.Id);
        int activePreset = -1;
        using (var reader = await cmd.ExecuteReaderAsync()) {
            if (reader.Read())
            {
                activePreset = reader.GetInt32(0);
            } else
            {
                throw new Exception("пользователь не найден");
            }
        }

        cmd = database.CreateCommand();
        cmd.CommandText = "SELECT name, outline_color, title_color, subtitle_color FROM user_presets WHERE id = $1";
        cmd.Parameters.AddWithValue("$1", activePreset);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.Read())
            {
                return new UserPreset()
                {
                    Id = activePreset,
                    OwnerId = u.Id,
                    Name = reader.GetString(0),
                    OutlineColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(1))),
                    TitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(2))),
                    SubtitleColor = System.Drawing.Color.FromArgb((int)(0xff000000L | reader.GetInt64(3))),
                };
            } else
            {
                throw new Exception("выбранный пресет пользователя " + u.Id + " не найден");
            }
        }
    }

    async Task SetActiveUserPreset(User u, long id)
    {
        var cmd = database.CreateCommand();
        cmd.CommandText = "UPDATE users SET selected_preset = $1 WHERE id = $2";
        cmd.Parameters.AddWithValue("$1", id);
        cmd.Parameters.AddWithValue("$2", u.Id);
        await cmd.ExecuteNonQueryAsync();
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
