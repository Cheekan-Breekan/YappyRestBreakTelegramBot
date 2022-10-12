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
                Console.WriteLine("Сообщение юзера: " + text);
                if (text.ToLower().Contains("delete"))
                {
                    var lineToDelete = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1));
                    messageProcess.DeleteLine(lineToDelete);
                    await PrepareAnswer(bot, chat);
                    return;
                }
                if (text.ToLower().Contains("help"))
                {
                    var rules = $"Количество обедов в один промежуток времени:{Environment.NewLine}Днем - максимум 10 (только 6 обедов).{Environment.NewLine}" +
                        $"Ночью - максимум 5 (только 3 обеда).";
                    await bot.SendTextMessageAsync(chat, $"Правила отправки сообщений.{Environment.NewLine}Заполните по следующему образцу:{Environment.NewLine}" +
                        $"Время перерыва (пробел) Фамилия/Имя (пробел) Количество минут перерыва.{Environment.NewLine}" +
                        $"Например:{Environment.NewLine}{Environment.NewLine}18:30 Путин 10{Environment.NewLine}{Environment.NewLine}" +
                        $"Если нужно поставить больше одного перерыва в одном сообщении, то просто пишите каждый новый перерыв в новую строку." +
                        $"{Environment.NewLine}Например:{Environment.NewLine}{Environment.NewLine}17:00 Байден 30{Environment.NewLine}20:30 Байден 10" +
                        $"{Environment.NewLine}{Environment.NewLine}" +
                        $"Если нужно удалить свой перерыв, то используйте ключевое слово delete. Например:{Environment.NewLine}{Environment.NewLine}" +
                        $"delete Зеленский 13:00 30{Environment.NewLine}{Environment.NewLine}" +
                        $"Фраза выше, отправленная в чат, удалит перерыв Зеленского на 13:00 из списка. При отправлении на удаление фраза должна совпадать 1 в 1 с фразой в списке, " +
                        $"лучше используйте ctrl+c. Удаляется лишь один перерыв за раз, мультистрочность не поддерживается.{Environment.NewLine}{Environment.NewLine}{rules}" +
                        $"{Environment.NewLine}{Environment.NewLine}Также другие ключевые слова:{Environment.NewLine}" +
                        $"help - вызов данного сообщения,{Environment.NewLine}" +
                        $"оффтоп - при наличии данного слова в вашем сообщении бот не будет реагировать на него,{Environment.NewLine}" +
                        $"reset - полное удаление списка с перерывами из памяти бота, использовать только при необходимости!");
                    return;
                }
                if (text.ToLower().Contains("оффтоп")) { return; }
                if (text == "reset")
                {
                    messageProcess.ClearList();
                    await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!");
                    return;
                }
                messageProcess.StartProcess(text);
                await PrepareAnswer(bot, chat);
            }
        }
        public static async Task PrepareAnswer(ITelegramBotClient bot, Chat chat)
        {
            var answer = messageProcess.GetFullMessage();
            var length = answer.Length;
            Console.WriteLine(length);
            await SendAnswer(bot, answer, length, chat);
        }
        public static async Task SendAnswer(ITelegramBotClient bot, string answer, int length, Chat chat)
        {
            try
            {
                if (length > 0 && length < 3000)
                    await bot.SendTextMessageAsync(chat, answer);
                else if (length > 3000)
                {
                    for (int i = 0; i < length; i += 3000)
                    {
                        var text = answer.Substring(i, Math.Min(3000, length - i));
                        Console.WriteLine(text);
                        if (string.IsNullOrEmpty(text)) //
                            Console.WriteLine("ПУСТО");
                        else
                            await bot.SendTextMessageAsync(chat, text);
                    }
                }
                else
                    await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправке ответного сообщения!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonSerializer.Serialize(ex.Message));
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