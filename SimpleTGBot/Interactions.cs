using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTGBot;

internal static class Interactions
{
    public const string greetingMessage = "Привет. Я - бот, который умеет делать демотиваторы. Нажми на одну из кнопок в меню или отправь картинку, чтобы продолжить.";
    public const string awaitingPictureMessage = "Присылай картинку, а я скажу, что делать дальше. Или можешь нажать на кнопку в меню.";
    public const string sayHelloMessage = "Напиши \"привет\" или нажми на кнопку в меню, чтобы начать.";
    public const string sendPictureOrQuitMessage = "Пришли мне картинку для демотиватора, чтобы продолжить. Чтобы отменить, напиши \"назад\" или \"отмена\"";
    public const string sendTitleOrQuitMessage = "Пришли мне текст для демотиватора, чтобы продолжить. Чтобы отменить, нажми на кнопку в меню.";
    public const string awaitingTitleMessage = "Шикарная картинка. Давай сделаем из неё крутой интернет-мэм для скуфов. Какой текст ты бы хотел видеть на нём? Можно написать две строки, тогда первая строка будет заголовком, а вторая под ним.";
    public const string showingResultMessage = "Вот такой демотиватор получился. Можно добавить водяной знак или оставить как есть (нажмите одну из кнопок).";
    public const string awaitingSubtitleMessage = "Найс. Напиши текст, который будет под заголовком, или точку (.), чтобы не добавлять его.";
    public const string chooseActionMessage = "Нажмите на одну из кнопок.";
    public const string settingsMessage = "Здесь можно:\n- настроить сохранённые стили";

    static readonly string[] helloWords = ["прив","привет","▶️начать","ку","хай","приветик","превед","привки","хаюхай","здравствуй","здравствуйте","здорово","дарова","дороу","здарова","здорова"];
    static readonly string[] cancelWords = ["↩️назад", "назад", "выйти", "отмена", "отменить", "отменяй", "галя", "галина", "стоп"];
    static readonly string[] settingsWords = ["⚙️настройки", "настройки", "настроить"];

    public static readonly string backButtonText = "↩️Назад";
    public static readonly string gotoPresetsButtonText = "🎨Сохранённые стили";
    public static readonly string doneButtonText = "✅Готово";

    public static readonly IReplyMarkup mainReplyMarkup = new ReplyKeyboardMarkup([[new KeyboardButton("▶️Начать")], [new KeyboardButton("⚙️Настройки")]]);
    public static readonly IReplyMarkup backButtonReplyMarkup = new ReplyKeyboardMarkup(new KeyboardButton("↩️Назад"));
    public static readonly IReplyMarkup resultActionReplyMarkup = new ReplyKeyboardMarkup([new KeyboardButton(doneButtonText)]);
    public static readonly IReplyMarkup settingsReplyMarkup = new ReplyKeyboardMarkup([[new KeyboardButton(gotoPresetsButtonText)], [new KeyboardButton(backButtonText)]]);

    static readonly string[] digitEmojis = ["0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣"];

    public static bool IsStartCommand(string message)
    {
        return message.Split(' ').FirstOrDefault() == "/start";
    }

    public static bool IsHello(string message)
    {
        string[] messageWords = message.ToLower().Split(new char[] { ' ', ',', '.', ';', '(', ')' });
        return helloWords.Any(word => messageWords.Contains(word));
    }

    public static bool IsGotoSettings(string message)
    {
        string[] messageWords = message.ToLower().Split(new char[] { ' ', ',', '.', ';', '(', ')' });
        return settingsWords.Any(word => messageWords.Contains(word));
    }

    public static bool IsCancellation(string message)
    {
        string[] messageWords = message.ToLower().Split(new char[] { ' ', ',', '.', ';', '(', ')' });
        return cancelWords.Any(word => messageWords.Contains(word));
    }

    public static bool IsCancelButton(string message)
    {
        return message == backButtonText;
    }

    public static string MakePresetListMessage(string[] presetNames)
    {
        StringBuilder msg = new StringBuilder();
        msg.Append("Твои сохранённые стили:\n");
        for (int i = 0; i < presetNames.Length; i++)
        {
            msg.Append(DigitsToEmoji((i + 1).ToString()));
            msg.Append(' ');
            msg.Append(presetNames[i]);
            msg.Append('\n');
        }
        if (presetNames.Length == 0)
        {
            msg.Append("<пусто>");
        }
        msg.Append("\n");
        return msg.ToString();
    }

    public static string DigitsToEmoji(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length * 4);
        foreach (char digit in s)
        {
            sb.Append(digitEmojis[digit - '0']);
        }
        return sb.ToString();
    }
}
