using System.Text.Json.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using Telegram.Bot.Extensions.Polling;

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
                Console.WriteLine("Сообщение юзера: " + message.Text);
                if (message.Text == "/start")
                {
                    await bot.SendTextMessageAsync(message.Chat, "Добро пожаловать!");
                    return;
                }
                messageProcess.StartProcess(message.Text);
                var answer = messageProcess.GetFullMessage();
                var length = answer.Length;
                Console.WriteLine(length);
                if (length > 0 /*&& length < 3000*/)
                    await bot.SendTextMessageAsync(message.Chat, answer);
                //else if (length > 3000)
                //{
                //    for (float i = 0f; i < length / 3000f; i++)
                //    {
                //        var text = answer.Substring(i, i)
                //    }
                //}
                else
                    await bot.SendTextMessageAsync(message.Chat, "Произошла ошибка, ответа нет.");
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