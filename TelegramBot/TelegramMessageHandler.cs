using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;
public class TelegramMessageHandler(IConfiguration config, ITelegramBotClient bot, Update update, Dictionary<long, BreaksHandler> chats, CancellationToken cToken)
{
    private static bool isAdmin = false;
    public async Task StartMessageHandleAsync()
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
            if (!CheckForWhitelistChat(chatId))
            {
                await bot.SendTextMessageAsync(chat, "Нет прав писать этому боту.", cancellationToken: cToken);
                Log.Warning($"Нет прав писать этому боту в чате {message.Chat.Id}: {message.Chat.Username} от {message.From.Id}: {message.From.Username}");
                return;
            }

            if (!chats.TryGetValue(chatId, out var breaksHandler))
            {
                breaksHandler = new BreaksHandler(chatId.ToString(), config);
                chats.Add(chatId, breaksHandler);
            }

            if (FileOperations.FileContainsValue(author, true))
            {
                isAdmin = true;
            }

            await CheckMessageForKeywords(bot, chatId, chat, text, id, breaksHandler, cToken);
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
            if (FileOperations.FileContainsValue(message.Chat.Id.ToString(), false)
                && FileOperations.FileContainsValue(message.From.Id.ToString(), true))
            {
                var file = await bot.GetFileAsync(doc.FileId);
                var fileName = AppDomain.CurrentDomain.BaseDirectory + docName;
                using var stream = new FileStream(fileName, FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath, stream, cToken);
                await bot.SendTextMessageAsync(message.Chat.Id,
                        $"Файл установлен.",
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

    private static bool CheckForWhitelistChat(long chatId)
    {
        if (!FileOperations.FileContainsValue(chatId.ToString(), isAdminCheck: false))
        {
            return false;
        }
        return true;
    }

    private async Task CheckMessageForKeywords(ITelegramBotClient bot, long chatId, Chat chat, string text, int id, BreaksHandler handler, CancellationToken cToken)
    {
        switch (text.ToLower())
        {
            case string a when a.Contains("оффтоп"): { break; }
            case string b when (b.Contains("delete") || b.Contains("удалить")):
                {
                    handler.StartProcessing(text, true);
                    await SendAnswer(bot, chat, id, handler);
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
                    await SendAnswer(bot, chat, id, handler);
                    break;
                }
            case string e when (e == "очистить"):
                {
                    if (isAdmin)
                    {
                        handler.ClearList();
                        await bot.SendTextMessageAsync(chat, "Список перерывов полностью очищен!", replyToMessageId: id, cancellationToken: cToken);
                        Log.Logger.Information("Список перерывов полностью очищен!", "BOT");
                    }
                    break;
                }
            case string f when f.Contains("вставить"):
                {
                    if (isAdmin)
                    {
                        handler.StartProcessing(text, isToInsert: true);
                        await SendAnswer(bot, chat, id, handler);
                    }
                    break;
                }
            case string g when g == "применить":
                {
                    if (isAdmin)
                    {
                        Log.Warning("Начало применения новых лимитов.");
                        handler.ApplyLimits();
                        await bot.SendTextMessageAsync(chatId,
                            $"Новые лимиты перерывов применены только в данном чате!",
                            cancellationToken: cToken);
                        Log.Warning("Окончание операции.");
                    }
                    break;
                }
            case string h when h == "скачать":
                {
                    if (isAdmin)
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
                    if (isAdmin)
                    {
                        FileOperations.RestoreDefault();
                        await bot.SendTextMessageAsync(chatId,
                            $"Восстановлены все значения по умолчанию. Для применения новых настроек напишите: применить.",
                            cancellationToken: cToken);
                        Log.Warning("Восстановление значений настроек по умолчанию.");
                    }
                    break;
                }
            case string l when l == "инструкция (админ)":
                {
                    if (isAdmin)
                    {
                        var guideMessage = PrepareGuideMessage();
                        var markup = CreateReplyKeyboard();
                        await bot.SendTextMessageAsync(chat, guideMessage, replyToMessageId: id, cancellationToken: cToken, replyMarkup: markup);
                    }
                    break;
                }
            default:
                {
                    handler.StartProcessing(text);
                    await SendAnswer(bot, chat, id, handler);
                    break;
                }
        }
    }
    private static ReplyKeyboardMarkup CreateReplyKeyboard()
    {
        return new(new[]
        {
            new KeyboardButton[] {"Помощь", "Список"},
            new KeyboardButton[] {"Инструкция (админ)"},
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
                            $"Днем с 12 до 16 - {config.GetValue<int>($"Limits:{chatId}:DinnersLimitDay")} обедов и " +
                            $"{config.GetValue<int>($"Limits:{chatId}:BreaksLimitDay")} десятиминуток, " +
                            $"в остальное дневное время {config.GetValue<int>($"Limits:{chatId}:DinnersLimitBetween")} обедов и " +
                            $"{config.GetValue<int>($"Limits:{chatId}:BreaksLimitBetween")} десятиминуток.{Environment.NewLine}" +
                            $"Ночью - только {config.GetValue<int>($"Limits:{chatId}:DinnersLimitNight")} обедов и " +
                            $"{config.GetValue<int>($"Limits:{chatId}:BreaksLimitNight")} десятиминуток.";
    }
    private static string PrepareGuideMessage()
    {
        return $"Всё нижеперечисленное доступно лишь тем, чьё ID телеграма прописано в файле access.json.{Environment.NewLine}" +
            $"Команда <<очистить>> полностью обнуляет список перерывов, полезно в случае ввода большого количества неверных данных.{Environment.NewLine}" +
            $"Команда <<восстановить>> полностью обнуляет все настройки бота для всех чатов. " +
            $"Полезно при повреждении любого из файлов настроек и невозможности его восстановить вручную.{Environment.NewLine}{Environment.NewLine}" +
            $"Настройки деляться на три типа:{Environment.NewLine}" +
            $"1) Настройка чатов, где бот может работать (лс и группы), название файла - {FileOperations.FileNameChats}. " +
            $"Здесь построчно находятся ID чатов. Добавление происходит простым копированием ID чата в отдельную строку(!!!), никакие лишние символы недопустимы! " +
            $"Удаление происходит простым удалением строки с ID чата. Добавление ID группы отличается от ID человека тем, что после копирования в файл нужно добавить в ID число 100 после минуса, " +
            $"например: ID чата перерывов Ритма в web-телеграме -1652853848, а в файле нужно будет -1001652853848.{Environment.NewLine}" +
            $"2) Настройка админов бота (только ID людей), название файла - {FileOperations.FileNameAccess}. Логика та же, что и в пункте 1, только без групповых чатов.{Environment.NewLine}" +
            $"3) Настройка лимитов перерывов/обедов для чата, название файла - {FileOperations.FileNameSettings}. В названии каждой секции присутствует ID чата, для которого определены лимиты ниже. " +
            $"Сначала идут обеды: день (12-16), ночь(22-6), между ними(6-12, 16-22). Далее также десятиминутки. " +
            $"Для изменения лимита нужно просто поменять число у нужной строки. Для добавления нового чата нужно просто скопировать и вставить одну из уже существующих секций, " +
            $"изменить ID в названии секции, изменить лимиты.{Environment.NewLine}{Environment.NewLine}" +
            $"Команда <<скачать>> позволяет скачать файлы настроек к себе для их редактирования. Данную команду лучше применять в лс для сохранения приватности настроек. " +
            $"После редактирования необходимо просто перекинуть нужный файл(-ы) обратно боту, он его(их) скачает, установит и обязательно выдаст ответ об успехе операции. " +
            $"Данную команду лучше применять в лс для сохранения приватности настроек, хоть и устанавливать настройки могут лишь лица с админ.доступом.{Environment.NewLine}" +
            $"Команда <<применить>> позволяет привести в действие новые настройки. Внимание, без этой команды бот будет работать по старым настройкам! " +
            $"Применяется только в групповом чате, где нужно применить настройки! Применение в одном чате не обновляет настройки в любом другом, это нужно помнить!";
    }
    private static async Task SendAnswer(ITelegramBotClient bot, Chat chat, int id, BreaksHandler messageProcess)
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
}
