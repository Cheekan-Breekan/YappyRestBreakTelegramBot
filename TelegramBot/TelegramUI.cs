using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;
public class TelegramUI
{
    //private const string token = "5605211357:AAFR7Ys8a5Ey6Sy5jL_tyS3S2iQKQVaw1tI"; //основной (бывший яппи)
    private const string token = "5620311832:AAGVmmVQE0rkz7NNfI28HKfo97ZLy2u3Arc"; //тестовый
    private readonly IConfiguration _config;
    private readonly ITelegramBotClient _telegramBot = new TelegramBotClient(token);

    private Dictionary<long, MessageProcess> chats;
    public TelegramUI(IConfiguration config)
    {
        _config = config;
    }
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cToken)
    {
        try
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

                Log.Information($"В чате: {chat.Username} ({chatId}) от {authorName} ({author}) сообщение: {text}");
                if (!chats.ContainsKey(chatId))
                {
                    await bot.SendTextMessageAsync(chat, "Нет прав писать этому боту.", cancellationToken: cToken);
                    Log.Warning("Нет прав писать этому боту.");
                    return;
                }
                var messageProcess = chats[chatId];
                if (text.ToLower().Contains("fromchatid"))
                {
                    var splitText = text.Split("\n");
                    var desiredChat = string.Concat(splitText[0].Skip(11));
                    messageProcess = chats[long.Parse(desiredChat)];
                    text = string.Join("\n", splitText.Skip(1));
                }
                await CheckMessageForKeywords(bot, chatId, chat, text, id, author, messageProcess, cToken);
            }
            else if (update.Type == UpdateType.Message && update?.Message?.Document is not null)
            {
                var message = update.Message;
                var doc = message.Document;
                var docName = doc.FileName;

                if (!FileOperations.CheckForFileName(docName))
                {
                    return;
                }

                Log.Warning($"Скачивание документа настроек {docName} в чате {message.Chat.Id}: {message.Chat.Username} от {message.From.Id}: {message.From.Username}");
                if (FileOperations.FileContainsValue(message.Chat.Id.ToString(), FileOperations.fileNameChats)
                    && FileOperations.FileContainsValue(message.From.Id.ToString()))
                {
                    var file = await bot.GetFileAsync(doc.FileId);
                    var fileName = AppDomain.CurrentDomain.BaseDirectory + docName;
                    using var stream = new FileStream(fileName, FileMode.Create);
                    await bot.DownloadFileAsync(file.FilePath, stream, cToken);
                    await bot.SendTextMessageAsync(message.Chat.Id,
                            $"Файл установлен. Для применения новых настроек напишите: применить.",
                            cancellationToken: cToken);
                }
                else
                {
                    Log.Warning($"Несанкционированный доступ к перезаписи настроек бота!!!");
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        $"У вас нет прав для данной операции! Товарищ @{message.From.Username}! Эй, тут кто-то балуется 😡",
                        replyToMessageId: message.MessageId);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Ошибка в {nameof(HandleUpdateAsync)}");
            await bot.SendTextMessageAsync(update?.Message?.Chat, "Произошла ошибка при отправлении ответного сообщения!" +
                "Проверьте правильность своего сообщения. Если ошибка не исправляется, то пожалуйста, сообщите о ней. 😰");
        }
    }
    private async Task CheckMessageForKeywords(ITelegramBotClient bot, long chatId, Chat chat, string text, int id, string? author, MessageProcess messageProcess, CancellationToken cToken)
    {
        switch (text.ToLower())
        {
            case string a when a.Contains("оффтоп"): { break; }
            case string b when (b.Contains("delete") || b.Contains("удалить")):
                {
                    messageProcess.StartProcessing(text, true);
                    await SendAnswer(bot, chat, id, messageProcess);
                    break;
                }
            case string c when (c == "help" || c == "помощь"):
                {
                    var helpMessage = PrepareHelpMessage(chatId);
                    var markup = CreateReplyKeyboard();
                    await bot.SendTextMessageAsync(chat, helpMessage, replyToMessageId: id, cancellationToken: cToken, replyMarkup: markup);
                    break;
                }
            case string d when d == "список":
                {
                    await SendAnswer(bot, chat, id, messageProcess);
                    break;
                }
            case string e when (e == "очистить"):
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        messageProcess.ClearList();
                        await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!", replyToMessageId: id, cancellationToken: cToken);
                        Log.Logger.Information("Список перерывов полностью очищен!", "BOT");
                    }
                    break;
                }
            case string f when f.Contains("вставить"):
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        messageProcess.StartProcessing(text, isToInsert: true);
                        await SendAnswer(bot, chat, id, messageProcess);
                    }
                    break;
                }
            case string g when g == "применить":
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        Log.Warning("Начало применения новых лимитов и добавления новых людей в whitelist доступа к боту.");
                        messageProcess.ApplyLimits();
                        LoadSavedChatsId();
                        //await bot.SendTextMessageAsync(chatId, $"Новые лимиты применены.{Environment.NewLine}{PrepareRulesMessage(chatId)}", cancellationToken: cToken);
                        await bot.SendTextMessageAsync(chatId,
                            $"Новые значения настроек доступа к боту в применены.\nНовые лимиты перерывов применены только в данном чате!",
                            cancellationToken: cToken);
                        Log.Warning("Окончание операции.");
                    }
                    break;
                }
            case string h when h == "скачать":
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        foreach (var fileName in FileOperations.CreateFileNamesArray())
                        {
                            using Stream stream = System.IO.File.OpenRead(fileName);
                            var iof = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, fileName);
                            await bot.SendDocumentAsync(chatId, iof, cancellationToken: cToken);
                        }
                        Log.Warning("Скачивание настроек.");
                    }
                    break;
                }
            case string j when j == "восстановить":
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        FileOperations.RestoreDefault();
                        await bot.SendTextMessageAsync(chatId,
                            $"Восстановлены все значения по умолчанию. Для применения новых настроек напишите: применить.",
                            cancellationToken: cToken);
                        Log.Warning("Восстановление значений настроек по умолчанию.");
                    }
                    break;
                }
            case string k when k == "всё ниже для админов!": { break; }
            case string l when l == "инструкция":
                {
                    if (FileOperations.FileContainsValue(author))
                    {
                        var guideMessage = PrepareGuideMessage();
                        var markup = CreateReplyKeyboard();
                        await bot.SendTextMessageAsync(chat, guideMessage, replyToMessageId: id, cancellationToken: cToken, replyMarkup: markup);
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
    private ReplyKeyboardMarkup CreateReplyKeyboard()
    {
        return new(new[]
        {
            new KeyboardButton[] {"Помощь", "Список"},
            new KeyboardButton[] {"Всё ниже для админов!"},
            new KeyboardButton[] {"Инструкция", "Очистить"},
            new KeyboardButton[] {"Скачать", "Применить"},
            new KeyboardButton[] {"Восстановить"},
        });
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
                            $"Запрещены цифры в имени/фамилии, а также пустые строки или длиной больше 50 символов. " +
                            $"Не нужно редактировать уже посланные сообщения, бот их не примет😢.{Environment.NewLine}" +
                            $"Обеды можно проставлять только в :00 и в :30 минут. Десятиминутки в минуты, кратные 10.{Environment.NewLine}" +
                            $"Сообщение с ключевым словом <<список>> позволяет просмотреть текущий список перерывов, не изменяя его.{Environment.NewLine}" +
                            $"Если нужно удалить свой перерыв, то используйте ключевое слово <<удалить>>. Например:{Environment.NewLine}{Environment.NewLine}" +
                            $"удалить 17:00 Цаль Виталий 30{Environment.NewLine}{Environment.NewLine}" +
                            $"Команда выше, отправленная в чат, удалит перерыв Цаля Виталия на 17:00 из списка. При удалении фраза должна совпадать 1 в 1 с фразой в списке, " +
                            $"лучше используйте ctrl+c." +
                            $"{Environment.NewLine}{Environment.NewLine}{rules}";
        return message;
    }
    private string PrepareRulesMessage(long chatId)
    {
        return $"Количество перерывов в один промежуток времени для данного чата:{Environment.NewLine}" +
                            $"Днем с 12 до 16 - {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitDay")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitDay")} десятиминуток, " +
                            $"в остальное дневное время {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitBetween")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitBetween")} десятиминуток.{Environment.NewLine}" +
                            $"Ночью - только {_config.GetValue<int>($"Limits:{chatId}:DinnersLimitNight")} обедов и " +
                            $"{_config.GetValue<int>($"Limits:{chatId}:BreaksLimitNight")} десятиминуток.";
    }
    private string PrepareGuideMessage()
    {
        return $"Всё нижеперечисленное доступно лишь тем, чьё ID телеграма прописано в файле access.json.{Environment.NewLine}" +
            $"Команда <<очистить>> полностью обнуляет список перерывов, полезно в случае ввода большого количества неверных данных.{Environment.NewLine}" +
            $"Команда <<восстановить>> полностью обнуляет все настройки бота для всех чатов. " +
            $"Полезно при повреждении любого из файлов настроек и невозможности его восстановить вручную.{Environment.NewLine}{Environment.NewLine}" +
            $"Настройки деляться на три типа:{Environment.NewLine}" +
            $"1) Настройка чатов, где бот может работать (лс и группы), название файла - {FileOperations.fileNameChats}. " +
            $"Здесь построчно находятся ID чатов. Добавление происходит простым копированием ID чата в отдельную строку(!!!), никакие лишние символы недопустимы! " +
            $"Удаление происходит простым удалением строки с ID чата. Добавление ID группы отличается от ID человека тем, что после копирования в файл нужно добавить в ID число 100 после минуса, " +
            $"например: ID чата перерывов Ритма в web-телеграме -1652853848, а в файле нужно будет -1001652853848.{Environment.NewLine}" +
            $"2) Настройка админов бота (только ID людей), название файла - {FileOperations.fileNameAccess}. Логика та же, что и в пункте 1, только без групповых чатов.{Environment.NewLine}" +
            $"3) Настройка лимитов перерывов/обедов для чата, название файла - {FileOperations.fileNameSettings}. В названии каждой секции присутствует ID чата, для которого определены лимиты ниже. " +
            $"Сначала идут обеды: день (12-16), ночь(22-6), между ними(6-12, 16-22). Далее также десятиминутки. " +
            $"Для изменения лимита нужно просто поменять число у нужной строки. Для добавления нового чата нужно просто скопировать и вставить одну из уже существующих секций, " +
            $"изменить ID в названии секции, изменить лимиты.{Environment.NewLine}{Environment.NewLine}" +
            $"Команда <<скачать>> позволяет скачать файлы настроек к себе для их редактирования. Данную команду лучше применять в лс для сохранения приватности настроек. " +
            $"После редактирования необходимо просто перекинуть нужный файл(-ы) обратно боту, он его(их) скачает, установит и выдаст ответ об успехе или неуспехе операции. " +
            $"Данную команду лучше применять в лс для сохранения приватности настроек, хоть и устанавливать настройки могут лишь лица с админ.доступом.{Environment.NewLine}" +
            $"Команда <<применить>> позволяет привести в действие новые настройки. Внимание, без этой команды бот будет работать по старым настройкам! " +
            $"Применяется только в групповом чате, где нужно применить настройки! Применение в одном чате не обновляет настройки в любом другом, это нужно помнить!";
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
            {
                await bot.SendTextMessageAsync(chat, "Произошла непредвиденная ошибка при отправлении ответного сообщения! Пожалуйста, сообщите о ней.", replyToMessageId: id);
                Log.Error("Ошибка! Не произошла отправка обратного сообщения!");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка! Не произошла отправка обратного сообщения!");
        }
    }
    private static Task HandleErrorsAsync(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
    {
        Log.Fatal(ex, "Ошибка! Фатальная ошибка приложения!!!");
        var error = ex switch
        {
            Telegram.Bot.Exceptions.ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => ex.ToString()
        };
        return Task.CompletedTask;
    }
    public void StartBot()
    {
        LoadSavedChatsId();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },
            ThrowPendingUpdates = false,
        };
        _telegramBot.StartReceiving(HandleUpdateAsync, HandleErrorsAsync, receiverOptions, cancellationToken);
        Log.Warning("Бот запущен: " + _telegramBot.GetMeAsync().Result.FirstName);
    }
    private void LoadSavedChatsId()
    {
        try
        {
            chats = new();
            foreach (var line in FileOperations.LoadChatIds())
            {
                if (long.TryParse(line, out var id))
                {
                    chats.Add(id, new MessageProcess(line, _config));
                    Log.Warning(id + " - айди чата загружен");
                }
                else { Log.Error(line + " - не удалось загрузить айди! Ошибка!"); }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal($"Произошла непредвиденная ошибка при загрузке id чата {ex}");
        }
    }
}
