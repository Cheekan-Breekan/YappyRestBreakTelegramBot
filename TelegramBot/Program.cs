using System.Text.Json.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using Telegram.Bot.Extensions.Polling;
using System;

namespace TelegramBot
{
    internal class Program
    {
        const string token = "5605211357:AAFR7Ys8a5Ey6Sy5jL_tyS3S2iQKQVaw1tI";
        static ITelegramBotClient telegramBot = new TelegramBotClient(token);
        static MessageProcess messageProcess = new MessageProcess();
        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                var chat = message.Chat;
                var text = message.Text;
                Console.WriteLine("Сообщение юзера: " + text);
                if (text.Contains("edit"))
                {
                    var lineToDelete = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1));
                    messageProcess.DeleteLine(lineToDelete);
                    await bot.SendTextMessageAsync(chat, "Успешно удалено!");
                    await PrepareAnswer(bot, chat);
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
                    await bot.SendTextMessageAsync(chat, "Произошла ошибка, ответа нет.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static async Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonSerializer.Serialize(ex.Message));
            Console.WriteLine(ex.Message);
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