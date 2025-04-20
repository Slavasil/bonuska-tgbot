using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTGBot;

internal static class Interactions
{
    public const string awaitingPictureMessage = "Привет. Я - бот, который умеет делать демотиваторы. Присылай картинку, а я скажу, что делать дальше. Или можешь нажать на кнопку в меню.";
    public const string sayHelloMessage = "Напиши \"привет\" или нажми на кнопку в меню, чтобы начать.";
    public const string sendPictureOrQuitMessage = "Пришли мне картинку для демотиватора, чтобы продолжить. Чтобы отменить, напиши \"назад\" или \"\"";

    public static readonly IReplyMarkup initialReplyMarkup = new ReplyKeyboardMarkup([[new KeyboardButton("▶️Начать")]]);
    public static readonly IReplyMarkup backButtonReplyMarkup = new ReplyKeyboardMarkup(new KeyboardButton("↩️Назад"));
    public static readonly IReplyMarkup quickActionReplyMarkup = new ReplyKeyboardRemove();

    static readonly string[] helloWords = ["прив","привет","▶️начать","ку","хай","приветик","превед","привки","хаюхай","здравствуй","здравствуйте","здорово","дарова","дороу","здарова","здорова"];
    static readonly string[] cancelWords = ["↩️назад", "назад", "выйти", "отмена", "отменить", "отменяй", "галя", "галина", "стоп"];

    public static bool IsStartCommand(string message)
    {
        return message.Split(' ').FirstOrDefault() == "/start";
    }

    public static bool IsHello(string message)
    {
        string[] messageWords = message.ToLower().Split(new char[] { ' ', ',', '.', ';', '(', ')' });
        return helloWords.Any(word => messageWords.Contains(word));
    }

    public static bool IsCancellation(string message)
    {
        string[] messageWords = message.ToLower().Split(new char[] { ' ', ',', '.', ';', '(', ')' });
        return cancelWords.Any(word => messageWords.Contains(word));
    }
}
