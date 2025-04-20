using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTGBot;

internal static class Interactions
{
    public const string awaitingPictureMessage = "Присылай картинку, а я скажу, что делать дальше. Или можешь нажать на кнопку в меню.";
    public const string sayHelloMessage = "Напиши \"привет\" или нажми на кнопку в меню, чтобы начать.";
    public const string sendPictureOrQuitMessage = "Пришли мне картинку для демотиватора, чтобы продолжить. Чтобы отменить, напиши \"назад\" или \"\"";
    public const string sendTitleOrQuitMessage = "Пришли мне текст для демотиватора, чтобы продолжить. Чтобы отменить, нажми на кнопку в меню.";
    public const string awaitingTitleMessage = "Шикарная картинка. Давай сделаем из неё крутой интернет-мэм для скуфов. Какой текст ты бы хотел видеть на нём? Можно написать две строки, тогда первая строка будет заголовком, а вторая под ним.";
    public const string showingResultMessage = "Вот такой демотиватор получился. Можно поменять цветовую схему, добавить водяной знак или оставить как есть (нажмите одну из кнопок).";
    public const string awaitingSubtitleMessage = "Найс. Напиши текст, который будет под заголовком, или точку (.), чтобы не добавлять его.";
    public const string chooseActionMessage = "Нажмите на одну из кнопок.";

    static readonly string[] helloWords = ["прив","привет","▶️начать","ку","хай","приветик","превед","привки","хаюхай","здравствуй","здравствуйте","здорово","дарова","дороу","здарова","здорова"];
    static readonly string[] cancelWords = ["↩️назад", "назад", "выйти", "отмена", "отменить", "отменяй", "галя", "галина", "стоп"];
    static readonly string cancelButtonText = "↩️Назад";

    public static readonly string doneButtonText = "✅Готово";

    public static readonly IReplyMarkup initialReplyMarkup = new ReplyKeyboardMarkup([[new KeyboardButton("▶️Начать")]]);
    public static readonly IReplyMarkup backButtonReplyMarkup = new ReplyKeyboardMarkup(new KeyboardButton("↩️Назад"));
    public static readonly IReplyMarkup quickActionReplyMarkup = new ReplyKeyboardRemove();
    public static readonly IReplyMarkup resultActionReplyMarkup = new ReplyKeyboardMarkup([new KeyboardButton(doneButtonText)]);

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

    public static bool IsCancelButton(string message)
    {
        return message == cancelButtonText;
    }
}
