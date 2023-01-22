global using Serilog;
using Microsoft.Extensions.Configuration;

namespace TelegramBot
{
    internal class Program
    {
        static void Main()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
            var config = builder.Build();

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Warning).WriteTo.File($"Logs{Path.DirectorySeparatorChar}log .txt", rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();

            var telegramUI = new TelegramUI(config);
            telegramUI.StartBot();
            Console.ReadLine();
        }
    }
}