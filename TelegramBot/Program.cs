using System.Text.Json.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using Telegram.Bot.Extensions.Polling;
using System;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;

namespace TelegramBot
{
    internal class Program
    {
        const string token = "5605211357:AAFR7Ys8a5Ey6Sy5jL_tyS3S2iQKQVaw1tI";
        //const string token = "5620311832:AAGVmmVQE0rkz7NNfI28HKfo97ZLy2u3Arc"; //тестовый
        static ITelegramBotClient telegramBot = new TelegramBotClient(token);
        static MessageProcess messageProcess = new MessageProcess();
        static Logger logger = new Logger();

        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                var message = update.Message;
                var chat = message.Chat;
                var text = message.Text;
                var id = message.MessageId;
                Console.WriteLine($"Сообщение от {message.From}: " + text);
                try
                {
                    logger.LogMessage(text, message.From.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                switch (text.ToLower())
                {
                    case string a when a.Contains("оффтоп"): { break; }
                    case string a when a.Contains("delete"):
                        {
                            messageProcess.DeleteLines(text);
                            await PrepareAnswer(bot, chat, id);
                            break;
                        }
                    case string b when b.Contains("help"):
                        {
                            var rules = $"Количество перерывов в один промежуток времени:{Environment.NewLine}Днем с 12 до 16 - 15 обедов и 7 десятиминуток," +
                            $"в остальное дневное время 10 обедов и 7 десятиминуток.{Environment.NewLine}" +
                            $"Ночью - только 5 обедов и 5 десятиминуток.";
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
                            await PrepareAnswer(bot, chat, id);
                            break;
                        }
                    default:
                        {
                            messageProcess.StartProcess(text);
                            await PrepareAnswer(bot, chat, id);
                            break;
                        }
                }
            }
        }
        public static async Task PrepareAnswer(ITelegramBotClient bot, Chat chat, int id)
        {
            var answer = messageProcess.GetFullMessage();
            var length = answer.Length;
            Console.WriteLine(length);
            await SendAnswer(bot, answer, length, chat, id);
        }
        public static async Task SendAnswer(ITelegramBotClient bot, string answer, int length, Chat chat, int id)
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
                    await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправке ответного сообщения! Пожалуйста, сообщите о ней.", replyToMessageId: id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
        {
            //Console.WriteLine(JsonSerializer.Serialize(ex.Message));
            Console.WriteLine(ex.Message);
            return Task.CompletedTask;
        }
        static void Main()
        {
            Console.WriteLine("Бот запущен: " + telegramBot.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = { }
            };
            telegramBot.StartReceiving(HandleUpdateAsync, HandleErrorsAsync, receiverOptions, cancellationToken);
            Console.ReadLine();
        }
    }
}