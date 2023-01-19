using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace TelegramBot;
public class TelegramUI
{
    //const string token = "5605211357:AAFR7Ys8a5Ey6Sy5jL_tyS3S2iQKQVaw1tI"; //яппи
    //const string token = "5836576057:AAHhYfo9sBbEiD2WQE5SuBZV7O2vsZcLZK8"; //цифромед
    private const string token = "5620311832:AAGVmmVQE0rkz7NNfI28HKfo97ZLy2u3Arc"; //тестовый
    private readonly IConfiguration _config;
    private readonly ITelegramBotClient _telegramBot = new TelegramBotClient(token);
    private const string accessFile = "access";
    private const string chatsFile = "chats";
    readonly Logger _logger = new(); //TODO: Реализовать уже логгер

    private Dictionary<long, MessageProcess> chats;
    public TelegramUI(IConfiguration config)
    {
        _config = config;
    }
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cToken)
    {
        if (update.Type == UpdateType.Message && update?.Message?.Text != null)
        {
            var message = update.Message;
            var chatId = message.Chat.Id;
            var chat = message.Chat;
            var text = message.Text;
            var id = message.MessageId;
            var author = message.From.Id.ToString();
            var authorName = message.From.Username;
            try
            {
                Console.WriteLine($"Сообщение от {authorName} в чате {chatId}: " + text);
                if (!chats.ContainsKey(chatId))
                {
                    await bot.SendTextMessageAsync(chat, "Нет прав писать этому боту.", cancellationToken: cToken);
                    return;
                }
                var messageProcess = chats[chatId];
                if (text.ToLower().Contains("fromchatid"))
                {
                    var splitText = text.Split("\n");
                    var desiredChat = string.Concat(splitText[0].Skip(11));
                    messageProcess = chats[long.Parse(desiredChat)];
                    text = string.Concat(splitText.Skip(1));
                }
                _logger.LogMessage(text, authorName);
                await CheckMessageForKeywords(bot, chatId, chat, text, id, author, messageProcess, cToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await bot.SendTextMessageAsync(chatId, "Произошла ошибка при отправлении ответного сообщения!" +
                    "Проверьте правильность своего сообщения. Если ошибка не исправляется, то пожалуйста, сообщите о ней. 😰");
            }
        }
        else if (update.Message.Document is not null) //TODO: Вынести в отдельный метод?
        {
            var message = update.Message;
            var doc = message.Document;
            if (!CheckForFileName(doc.FileName)) { return; }
            if (FileOperations.ReadId(chatsFile).Contains(message.Chat.Id.ToString()) 
                && FileOperations.ReadId(accessFile).Contains(message.From.Id.ToString()))
            {
                var file = await bot.GetFileAsync(doc.FileId);
                var fileName = AppDomain.CurrentDomain.BaseDirectory + doc.FileName;
                using var stream = new FileStream(fileName, FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath, stream, cToken);
            }
            else
            {
                await bot.SendTextMessageAsync(message.Chat.Id, 
                    $"У вас нет прав для данной операции! Товарищ @{message.From.Username}! Эй, @MrSega13, тут кто-то балуется 😡",
                    replyToMessageId: message.MessageId);
            }
        }
    }
    private async Task CheckMessageForKeywords(ITelegramBotClient bot, long chatId, Chat chat, string text, int id, string? author, MessageProcess messageProcess, CancellationToken cToken)
    {
        switch (text.ToLower()) //TODO: Вынести админские методы в отдельный if блок?
        {
            case string a when a.Contains("оффтоп"): { break; }
            case string b when b.Contains("delete"):
                {
                    messageProcess.StartProcessing(text, true);
                    await SendAnswer(bot, chat, id, messageProcess);
                    break;
                }
            case string c when c.Contains("help"):
                {
                    var helpMessage = PrepareHelpMessage(chatId);
                    await bot.SendTextMessageAsync(chat, helpMessage, replyToMessageId: id, cancellationToken: cToken);
                    break;
                }
            case string d when d.Contains("список"):
                {
                    await SendAnswer(bot, chat, id, messageProcess);
                    break;
                }
            case string e when e.Contains("reset"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        messageProcess.ClearList();
                        await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!", replyToMessageId: id, cancellationToken: cToken);
                        _logger.LogMessage("Список перерывов полностью очищен!", "BOT");
                    }
                    break;
                }
            case string f when f.Contains("insert"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        messageProcess.StartProcessing(text, isToInsert: true);
                        await SendAnswer(bot, chat, id, messageProcess);
                    }
                    break;
                }
            case string g when g.Contains("applylimits"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        messageProcess.ApplyLimits();
                        await bot.SendTextMessageAsync(chatId, $"Новые лимиты применены.{Environment.NewLine}{PrepareRulesMessage(chatId)}", cancellationToken: cToken);
                    }
                    break;
                }
            case string h when h.Contains("download"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        var fileName = string.Concat(text.Skip(9)) + ".json";
                        using Stream stream = System.IO.File.OpenRead(fileName);
                        var iof = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, fileName);
                        await bot.SendDocumentAsync(chatId, iof, cancellationToken: cToken);
                    }
                    break;
                }
            case string j when j.Contains("restorebackup"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        FileOperations.RestoreDefault();
                        await bot.SendTextMessageAsync(chatId,
                            $"Восстановлены все значения по умолчанию. Для применения лимитов используйте команду applylimits. Для перезагрузки прав доступа - applyids",
                            cancellationToken: cToken);
                    }
                    break;
                }
            case string k when k.Contains("applyids"):
                {
                    if (FileOperations.ReadId(accessFile).Contains(author))
                    {
                        LoadSavedChatsId();
                        await bot.SendTextMessageAsync(chatId,
                            $"Новые значения доступа к боту применены.",
                            cancellationToken: cToken);
                    }
                    break;
                }
            default:
                {
                    messageProcess.StartProcessing(text);
                    await SendAnswer(bot, chat, id, messageProcess);
                    break;
                }
        }
    }
    private bool CheckForFileName(string fileName)
    {
        var list = new List<string>()
        {
            $"{chatsFile}.json",
            $"{accessFile}.json",
            "appsettings.json"
        };
        if (list.Any(s => s.Contains(fileName)))
            return true;
        return false;
    }
    private string PrepareHelpMessage(long chatId)
    {
        var rules = PrepareRulesMessage(chatId);
        var message = $"Правила отправки сообщений!{Environment.NewLine}Заполните по следующему образцу:{Environment.NewLine}" +
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
                            $"{Environment.NewLine}{Environment.NewLine}{rules}";
        return message;
    }
    private string PrepareRulesMessage(long chatId)
    {
        return $"Количество перерывов в один промежуток времени:{Environment.NewLine}" +
                            $"Днем с 12 до 16 - {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitDay")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitDay")} десятиминуток, " +
                            $"в остальное дневное время {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitBetween")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitBetween")} десятиминуток.{Environment.NewLine}" +
                            $"Ночью - только {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitNight")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitNight")} десятиминуток.";
    }
    private async Task SendAnswer(ITelegramBotClient bot, Chat chat, int id, MessageProcess messageProcess)
    {
        var limit = 3800;
        var answer = messageProcess.GetFullMessage();
        var length = answer.Length;
        try
        {
            if (length > 0 && length < limit)
                await bot.SendTextMessageAsync(chat, answer, replyToMessageId: id);
            else if (length > limit)
            {
                var regex = new Regex($@"(?s).{{1,{limit}}}$", RegexOptions.Multiline);
                var matches = regex.Matches(answer);
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Value))
                        await bot.SendTextMessageAsync(chat, match.Value.TrimStart(), replyToMessageId: id);
                }
            }
            else
                await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправлении ответного сообщения! Пожалуйста, сообщите о ней.", replyToMessageId: id);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
    {
        Console.WriteLine(ex.Message);
        return Task.CompletedTask;
    }
    public void StartBot()
    {
        Console.WriteLine("Бот запущен: " + _telegramBot.GetMeAsync().Result.FirstName);
        LoadSavedChatsId();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = { }
        };
        _telegramBot.StartReceiving(HandleUpdateAsync, HandleErrorsAsync, receiverOptions, cancellationToken);
    }
    private void LoadSavedChatsId()
    {
        try
        {
            chats = new();
            foreach (var line in FileOperations.ReadId(chatsFile))
            {
                if (long.TryParse(line, out var id))
                {
                    chats.Add(id, new MessageProcess(line, _config));
                    Console.WriteLine(id + " - айди чата загружен");
                }
                else { Console.WriteLine(line + " - не удалось загрузить айди! Ошибка!"); }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла непредвиденная ошибка при загрузке id чата {ex}");
        }
    }
}
