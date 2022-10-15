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
        static ITelegramBotClient telegramBot = new TelegramBotClient(token);
        static MessageProcess messageProcess = new MessageProcess();
        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                var message = update.Message;
                var chat = message.Chat;
                var text = message.Text;
                var id = message.MessageId;
                Console.WriteLine("Сообщение юзера: " + text);
                if (text.ToLower().Contains("delete"))
                {
                    var lineToDelete = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1));
                    messageProcess.DeleteLine(lineToDelete);
                    await PrepareAnswer(bot, chat, id);
                    return;
                }
                if (text.ToLower().Contains("help"))
                {
                    var rules = $"Количество перерывов в один промежуток времени:{Environment.NewLine}Днем - максимум 14 (только 7 обедов и 7 десятиминуток).{Environment.NewLine}" +
                        $"Ночью - максимум 6 (только 3 обеда и 3 десятиминутки).";
                    await bot.SendTextMessageAsync(chat, $"Правила отправки сообщений.{Environment.NewLine}Заполните по следующему образцу:{Environment.NewLine}" +
                        $"Время перерыва (пробел) Фамилия/Имя (пробел) Количество минут перерыва.{Environment.NewLine}" +
                        $"Например:{Environment.NewLine}{Environment.NewLine}18:30 Цаль Виталий 10{Environment.NewLine}{Environment.NewLine}" +
                        $"Если нужно поставить больше одного перерыва в одном сообщении, то просто пишите каждый новый перерыв в новую строку." +
                        $"{Environment.NewLine}Например:{Environment.NewLine}{Environment.NewLine}17:00 Должанский Николай 30{Environment.NewLine}20:30 Должанский Николай 10" +
                        $"{Environment.NewLine}{Environment.NewLine}" +
                        $"Обеды можно проставлять только в :00 и в :30 минут. Десятиминутки в :00 или :10 минут." +
                        $"Если нужно удалить свой перерыв, то используйте ключевое слово delete. Например:{Environment.NewLine}{Environment.NewLine}" +
                        $"delete 13:00 Роман Мальцев 30{Environment.NewLine}{Environment.NewLine}" +
                        $"Фраза выше, отправленная в чат, удалит перерыв Романа Мальцева на 13:00 из списка. При удалении фраза должна совпадать 1 в 1 с фразой в списке, " +
                        $"лучше используйте ctrl+c. Удаляется лишь один перерыв за раз, мультистрочность не поддерживается." +
                        $"{Environment.NewLine}{Environment.NewLine}{rules}", replyToMessageId:id); /*+*/
                        //$"{Environment.NewLine}{Environment.NewLine}Также другие ключевые слова:{Environment.NewLine}" +
                        //$"help - вызов данного сообщения,{Environment.NewLine}" +
                        //$"оффтоп - при наличии данного слова в вашем сообщении бот не будет реагировать на него,{Environment.NewLine}" +
                        //$"reset - полное удаление списка с перерывами из памяти бота, использовать только при необходимости!");
                    return;
                }
                if (text.ToLower().Contains("оффтоп")) { return; }
                if (text == "reset")
                {
                    messageProcess.ClearList();
                    await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!", replyToMessageId:id);
                    return;
                }
                messageProcess.StartProcess(text);
                await PrepareAnswer(bot, chat, id);
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
                    await bot.SendTextMessageAsync(chat, answer, replyToMessageId:id);
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
                    await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправке ответного сообщения!", replyToMessageId: id);
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