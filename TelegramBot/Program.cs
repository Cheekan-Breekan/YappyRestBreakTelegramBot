using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Encodings.Web;
using Telegram.Bot.Types;

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
        static void BuildConfiguration(IConfigurationBuilder builder)
        {
            

        }
    }
}