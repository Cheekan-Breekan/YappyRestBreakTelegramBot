using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Microsoft.Extensions.Configuration;

namespace TelegramBot;
public class TelegramUI
{
    //const string token = "5605211357:AAFR7Ys8a5Ey6Sy5jL_tyS3S2iQKQVaw1tI"; //яппи
    //const string token = "5836576057:AAHhYfo9sBbEiD2WQE5SuBZV7O2vsZcLZK8"; //цифромед
    private const string token = "5620311832:AAGVmmVQE0rkz7NNfI28HKfo97ZLy2u3Arc"; //тестовый
    private readonly IConfiguration _config;
    private ITelegramBotClient telegramBot = new TelegramBotClient(token);
    Logger logger = new Logger();

    private Dictionary<long, MessageProcess> chats = new();
    public TelegramUI(IConfiguration config)
    {
        _config = config;
    }
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update?.Message?.Text != null)
        {
            var message = update.Message;
            var chatId = message.Chat.Id;
            var chat = message.Chat;
            var text = message.Text;
            var id = message.MessageId;
            Console.WriteLine($"Сообщение от {message.From} в чате {chatId}: " + text);
            if (!chats.ContainsKey(chatId))
            {
                await bot.SendTextMessageAsync(chat, "Нет прав писать этому боту.");
                return;
            }
            var messageProcess = chats[chatId];
            try
            {
                logger.LogMessage(text, message.From.ToString());

                switch (text.ToLower())
                {
                    case string a when a.Contains("оффтоп"): { break; }
                    case string b when b.Contains("delete"):
                        {
                            messageProcess.StartProcessing(text, true);
                            await PrepareAnswer(bot, chat, id, messageProcess);
                            break;
                        }
                    case string c when c.Contains("help"):
                        {
                            var rules = $"Количество перерывов в один промежуток времени:{Environment.NewLine}" +
                            $"Днем с 12 до 16 - {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitDay")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitDay")} десятиминуток," +
                            $"в остальное дневное время {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitBetween")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitBetween")} десятиминуток.{Environment.NewLine}" +
                            $"Ночью - только {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitNight")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitNight")} десятиминуток.";

                            await bot.SendTextMessageAsync(chat, $"Правила отправки сообщений!{Environment.NewLine}Заполните по следующему образцу:{Environment.NewLine}" +
                            $"Время перерыва (пробел) Фамилия/Имя (пробел) Количество минут перерыва.{Environment.NewLine}" +
                            $"Например:{Environment.NewLine}{Environment.NewLine}18:30 Цаль Виталий 10{Environment.NewLine}{Environment.NewLine}" +
                            $"Если нужно поставить больше одного перерыва в одном сообщении, то просто пишите каждый новый перерыв в новую строку (именно новая строка, а не сотня пробелов😉)." +
                            $"{Environment.NewLine}Например:{Environment.NewLine}{Environment.NewLine}17:00 Цаль Виталий 30{Environment.NewLine}20:30 Цаль Виталий 10" +
                            $"{Environment.NewLine}{Environment.NewLine}" +
                            $"{Environment.NewLine}Запрещены цифры в имени/фамилии, а также пустые строки или длиной больше 50 символов. " +
                            $"Не нужно редактировать уже посланные сообщения, бот их не примет😢.{Environment.NewLine}" +
                            $"Обеды можно проставлять только в :00 и в :30 минут. Десятиминутки в минуты, кратные 10.{Environment.NewLine}" +
                            $"Сообщение с ключевым словом <<список>> позволяет просмотреть текущий список перерывов, не изменяя его.{Environment.NewLine}" +
                            $"Если нужно удалить свой перерыв, то используйте ключевое слово <<delete>>. Например:{Environment.NewLine}{Environment.NewLine}" +
                            $"delete 17:00 Цаль Виталий 30{Environment.NewLine}{Environment.NewLine}" +
                            $"Фраза выше, отправленная в чат, удалит перерыв Цаля Виталия на 17:00 из списка. При удалении фраза должна совпадать 1 в 1 с фразой в списке, " +
                            $"лучше используйте ctrl+c." +
                            $"{Environment.NewLine}{Environment.NewLine}{rules}", replyToMessageId: id);
                            break;
                        }
                    case string d when d.Contains("reset"):
                        {
                            messageProcess.ClearList();
                            await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!", replyToMessageId: id);
                            break;
                        }
                    case string e when e.Contains("список"):
                        {
                            await PrepareAnswer(bot, chat, id, messageProcess);
                            break;
                        }
                    case string f when f.Contains("insert"):
                        {
                            messageProcess.StartProcessing(text, isToInsert: true);
                            await PrepareAnswer(bot, chat, id, messageProcess);
                            break;
                        }
                    case string g when g.Contains("addchatid"):
                        {
                            if (SaveNewChatId(text))
                                await bot.SendTextMessageAsync(chat, "Айди успешно добавлен");
                            else
                                await bot.SendTextMessageAsync(chat, "Не удалось добавить айди! Проверьте правильность сообщения: \"addchatid АЙДИ\"");
                            break;
                        }
                    default:
                        {
                            messageProcess.StartProcessing(text);
                            await PrepareAnswer(bot, chat, id, messageProcess);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
    private async Task PrepareAnswer(ITelegramBotClient bot, Chat chat, int id, MessageProcess messageProcess)
    {
        var answer = messageProcess.GetFullMessage();
        var length = answer.Length;
        Console.WriteLine(length);
        await SendAnswer(bot, answer, length, chat, id);
    }
    private async Task SendAnswer(ITelegramBotClient bot, string answer, int length, Chat chat, int id)
    {
        try
        {
            if (length > 0 && length < 3000)
                await bot.SendTextMessageAsync(chat, answer, replyToMessageId: id);
            else if (length > 3000)
            {
                for (int i = 0; i < length; i += 3000)
                {
                    var text = answer.Substring(i, Math.Min(3000, length - i));
                    Console.WriteLine(text);
                    if (string.IsNullOrEmpty(text)) //
                        Console.WriteLine("ПУСТО");
                    else
                        await bot.SendTextMessageAsync(chat, text, replyToMessageId: id);
                }
            }
            else
                await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправлении ответного сообщения! Пожалуйста, сообщите о ней.", replyToMessageId: id);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
    {
        Console.WriteLine(ex.Message);
        return Task.CompletedTask;
    }
    public void StartBot()
    {
        Console.WriteLine("Бот запущен: " + telegramBot.GetMeAsync().Result.FirstName);
        LoadSavedChatsId();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = { }
        };
        telegramBot.StartReceiving(HandleUpdateAsync, HandleErrorsAsync, receiverOptions, cancellationToken);
    }
    private void LoadSavedChatsId()
    {
        try
        {
            foreach (var line in FileOperations.ReadChatsId())
            {
                long id = Convert.ToInt64(line);
                chats.Add(id, new MessageProcess(line, _config));
                Console.WriteLine(id + " - айди чата загружен");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла непредвиденная ошибка при загрузке id чата {ex}");
        }
        
    }
    private bool SaveNewChatId(string message)
    {
        try
        {
            var strId = message.Replace("addchatid", "").Trim();
            if (long.TryParse(strId, out long id))
            {

                foreach (var line in FileOperations.ReadChatsId())
                {
                    if (line == id.ToString())
                        return false;
                }
                FileOperations.WriteChatId(id);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return false;
    }
}
