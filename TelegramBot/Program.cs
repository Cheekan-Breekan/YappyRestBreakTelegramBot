using Microsoft.Extensions.Configuration;

namespace TelegramBot
{
    internal class Program
    {
        static void Main()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
            var config = builder.Build();
            var telegramUI = new TelegramUI(config);
            telegramUI.StartBot();
            Console.ReadLine();
        }
    }
}