using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot;
public class TelegramHandler
{
    private readonly IConfiguration _config;
    public Dictionary<long, BreaksHandler> chats = [];
    public TelegramHandler(IConfiguration config)
    {
        _config = config;
    }
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cToken)
    {
        try
        {
            var handler = new TelegramMessageHandler(_config, bot, update, chats, cToken);
            await handler.StartMessageHandleAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Ошибка в {nameof(HandleUpdateAsync)}");
            await bot.SendTextMessageAsync(update?.Message?.Chat, "Произошла ошибка при отправлении ответного сообщения!" +
                "Проверьте правильность своего сообщения. Если ошибка не исправляется, то пожалуйста, сообщите о ней. 😰", cancellationToken: cToken);
        }
    }
    private static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
    {
        Log.Fatal(ex, "Ошибка! Фатальная ошибка приложения!!!");
        return Task.CompletedTask;
    }
    public void StartBot()
    {
        var id = _config["telegramIdTest"];
        var telegramBot = new TelegramBotClient(id);

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates =
            [
                UpdateType.Message,
            ],
            ThrowPendingUpdates = false,
        };
        telegramBot.StartReceiving(HandleUpdateAsync, HandleErrorsAsync, receiverOptions, cancellationToken);
        Log.Warning("Бот запущен: " + telegramBot.GetMeAsync().Result.FirstName);
    }
}
