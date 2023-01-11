using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
    internal class Program
    {
        static void Main()
        {
            var telegramUI = new TelegramUI();
            telegramUI.StartBot();
            Console.ReadLine();
        }
    }
}