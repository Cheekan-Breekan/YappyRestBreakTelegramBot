using Microsoft.Extensions.Configuration;
using System.Globalization;
using TelegramBot.Models;

namespace TelegramBot
{
    public class MessageProcess
    {
        private readonly IConfiguration _config;
        private readonly string _chatId;
        private List<UserMessageLine> MessageLines { get; set; } = new();
        private List<UserMessageLine> InputedMessageLines { get; set; }
        public bool IsErrorDetected { get; private set; }
        public string ErrorMessage { get; private set; }
        private string EndOfErrorMessage { get; } = $"Измените этот конкретный перерыв и отправьте его заново.{Environment.NewLine}{Environment.NewLine}";
        private int DinnersLimitDay { get; set; }
        private int DinnersLimitNight { get; set; }
        private int DinnersLimitBetween { get; set; }
        private int BreaksLimitDay { get; set; }
        private int BreaksLimitNight { get; set; }
        private int BreaksLimitBetween { get; set; }
        public MessageProcess(string chatId, IConfiguration config)
        {
            _chatId = chatId;
            _config = config;
            ApplyLimits();
        }
        public void ApplyLimits()
        {
            DinnersLimitDay = _config.GetValue<int>($"Limits:{_chatId}:DinnersLimitDay");
            DinnersLimitNight = _config.GetValue<int>($"Limits:{_chatId}:DinnersLimitNight");
            DinnersLimitBetween = _config.GetValue<int>($"Limits:{_chatId}:DinnersLimitBetween");
            BreaksLimitDay = _config.GetValue<int>($"Limits:{_chatId}:BreaksLimitDay");
            BreaksLimitNight = _config.GetValue<int>($"Limits:{_chatId}:BreaksLimitNight");
            BreaksLimitBetween = _config.GetValue<int>($"Limits:{_chatId}:BreaksLimitBetween");
        }
        public void StartProcessing(string messageText, bool isToDelete = false, bool isToInsert = false)
        {
            ErrorMessage = String.Empty;
            IsErrorDetected = false;
            InputedMessageLines = new();
            if (isToDelete)
            {
                ProcessInputData(messageText, true);
                DeleteLines();
                return;
            }
            else if (isToInsert)
            {
                ProcessInputData(messageText, true);
            }
            else
            {
                ProcessInputData(messageText, false);
            }
            if (InputedMessageLines.Count != 0)
                ChecksAndInsertLines();
        }
        private void ProcessInputData(string messageText, bool isHasKeyword)
        {
            if (isHasKeyword)
            {
                messageText = messageText.Replace("delete", string.Empty, StringComparison.CurrentCultureIgnoreCase);
                messageText = messageText.Replace("insert", string.Empty, StringComparison.CurrentCultureIgnoreCase);
                messageText = messageText.Replace("удалить", string.Empty, StringComparison.CurrentCultureIgnoreCase);
                messageText = messageText.Replace("вставить", string.Empty, StringComparison.CurrentCultureIgnoreCase);
            }
            var lines = messageText.Trim().Split($"\n").ToList();
            if (lines.Count > 7 && !isHasKeyword)
            {
                IsErrorDetected = true;
                ErrorMessage = "Неправильный ввод данных. Список перерывов никак не изменён! Слишком много перерывов, добавляйте лишь свои! ";
                return;
            }
            foreach (var line in lines.ToList())
            {
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                DateTime date = new DateTime();
                int time = 0;
                string name;
                var splittedLine = line.Trim().Split(' ');
                try
                {
                    date = ConvertToDate(splittedLine[0]);
                    time = int.Parse(splittedLine.Last());
                    var nonReadyName = string.Join(' ', splittedLine.Skip(1).SkipLast(1)).Trim();
                    name = ConvertToName(nonReadyName);
                }
                catch (Exception ex)
                {
                    Log.Information(ex.Message);
                    Log.Debug(ex.ToString());
                    IsErrorDetected = true;
                    if (date == default)
                    {
                        ErrorMessage += $"Грамматически неправильный перерыв " +
                            $"{(line.Length > 30 ? "с неизвестным временем" : $"({line})")}! {ex.Message} {EndOfErrorMessage}";
                    }
                    else if (time == default)
                    {
                        ErrorMessage += $"Грамматически неправильный перерыв " +
                            $"{(line.Length > 30 ? "с неизвестным временем" : $"({line})")}! Неправильно указана длительность перерыва. {EndOfErrorMessage}";
                    }
                    else
                    {
                        ErrorMessage += $"Грамматически неправильный перерыв {(line.Length > 30 ? $"в {date:HH:mm}" : $"({line})")}!" +
                            $" {ex.Message} {EndOfErrorMessage}";
                    }
                    lines.Remove(line);
                    continue;
                }
                InputedMessageLines.Add(new UserMessageLine(date, name, time));
            }
        }
        private void ChecksAndInsertLines()
        {
            DeleteOldDates();
            foreach (var lineToInsert in InputedMessageLines)
            {
                var date = lineToInsert.DinnerDate;
                var time = lineToInsert.Minutes;
                if (!CheckForRightDinnerTime(date, time))
                    continue;
                if (MessageLines.Count == 0)
                {
                    MessageLines.Add(lineToInsert);
                    continue;
                }
                var counter = 0;
                foreach (var line in MessageLines)
                {
                    if (line.DinnerDate == date && line.Minutes == time)
                        counter++;
                    if (CheckForDublicate(lineToInsert.ToString(), line.ToString(), date) && CheckForFreeSlots(date, time, counter))
                    {
                        if (MessageLines.First().DinnerDate > date)
                        {
                            MessageLines.Insert(0, lineToInsert);
                            break;
                        }
                        if (line.DinnerDate > date)
                        {
                            var index = MessageLines.IndexOf(line);
                            MessageLines.Insert(index, lineToInsert);
                            break;
                        }
                        else if (line.DinnerDate <= date && MessageLines.IndexOf(line) == MessageLines.Count - 1)
                        {
                            MessageLines.Add(lineToInsert);
                            break;
                        }
                    }
                    else { break; }
                }
            }
        }
        private bool CheckForDublicate(string lineToInsert, string currentLine, DateTime date)
        {
            if (lineToInsert == currentLine)
            {
                IsErrorDetected = true;
                ErrorMessage += $"В {date:HH:mm} уже проставлен идентичный перерыв! Не дублируйте! Правильные перерывы записаны.{Environment.NewLine}{Environment.NewLine}";
                return false;
            }
            return true;
        }
        private bool CheckForRightDinnerTime(DateTime date, int time)
        {
            var rightTime = time == 10 || time == 30;
            int dateMinutes = date.Minute;
            if (time == 10 && (dateMinutes % 10 == 0))
                return true;
            if (time == 30 && (0 == dateMinutes || 30 == dateMinutes))
                return true;
            IsErrorDetected = true;
            ErrorMessage += $"Неправильный перерыв в {date:H:mm}! Измените этот конкретный перерыв и отправьте его заново. " +
                $"Правильные перерывы записаны!{Environment.NewLine}{(rightTime ? "" : "Перерывы могут быть либо 10 минут, либо 30. ")}" +
                $"{(time == 30 ? "Обеды допустимы только в :00 минут и в :30 минут." : "")}" +
                $"{(time == 10 ? "Десятиминутки допустимы в любую минуту, кратную 10." : "")}{Environment.NewLine}{Environment.NewLine}";
            return false;
        }
        private bool CheckForFreeSlots(DateTime date, int time, int counter)
        {
            var dinnerLimit = (date.Hour >= 22 || date.Hour < 6) ? DinnersLimitNight : (date.Hour >= 12 && date.Hour <= 16) ? DinnersLimitDay : DinnersLimitBetween;
            var breakLimit = (date.Hour >= 22 || date.Hour < 6) ? BreaksLimitNight : (date.Hour >= 12 && date.Hour <= 16) ? BreaksLimitDay : BreaksLimitBetween;
            var isTenMinutes = time == 10;

            if (isTenMinutes && counter >= breakLimit)
            {
                IsErrorDetected = true;
                ErrorMessage += $"В {date:H:mm} уже {breakLimit} десятиминуток. Выберите для этого конкретного перерыва другое время и отправьте его заново. " +
                    $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                return false;
            }
            if (!isTenMinutes && counter >= dinnerLimit)
            {
                IsErrorDetected = true;
                ErrorMessage += $"В {date:H:mm} уже {dinnerLimit} обедов. Выберите для этого конкретного перерыва другое время и отправьте его заново. " +
                    $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                return false;
            }
            return true;
        }
        private void DeleteOldDates()
        {
            foreach (var line in MessageLines.ToList())
            {
                if (line.DinnerDate < DateTime.Now)
                    MessageLines.Remove(line);
                else
                    return;
            }
        }
        public string GetFullMessage()
        {
            if (IsErrorDetected)
            {
                IsErrorDetected = false;
                Log.Information(ErrorMessage);
                return ErrorMessage += "Используйте команду \"help\" для помощи и команду \"список\" для отображения актуального списка.";
            }
            DeleteOldDates();
            string fullMessage = string.Empty;
            foreach (var line in MessageLines)
            {
                fullMessage += line + Environment.NewLine;
            }
            fullMessage = string.IsNullOrWhiteSpace(fullMessage) ? "Список перерывов пуст." : fullMessage;
            return fullMessage;
        }
        private void DeleteLines()
        {
            DeleteOldDates();
            foreach (var lineToDelete in InputedMessageLines.ToList())
            {
                var textLineToDelete = lineToDelete.ToString();
                foreach (var line in MessageLines.ToList())
                {
                    if (textLineToDelete == line.ToString())
                    {
                        MessageLines.Remove(line);
                        InputedMessageLines.Remove(lineToDelete);
                        ErrorMessage += $"Перерыв {lineToDelete} удалён.{Environment.NewLine}{Environment.NewLine}";
                        break;
                    }
                }
            }
            if (InputedMessageLines.Count != 0)
            {
                IsErrorDetected = true;
                foreach (var line in InputedMessageLines)
                {
                    ErrorMessage += $"Перерыв {string.Join(' ', line.ToString().Split(' ').Skip(1))} в списке не найден!{Environment.NewLine}{Environment.NewLine}";
                }
            }
        }
        public void ClearList() => MessageLines.Clear();
        private string ConvertToName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { throw new Exception("Имя/Фамилия пусты."); }
            if (name.Length > 50) { throw new Exception("Слишком длинные имя/фамилия."); }
            if (name.Any(char.IsDigit)) { throw new Exception("Имя/Фамилия содержат цифры."); }
            return name;
        }
        private DateTime ConvertToDate(string time)
        {
            if (DateTime.TryParseExact(time, "H:m", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime date))
            {
                if (date < DateTime.Now)
                    date += TimeSpan.FromHours(24);
                if (date - DateTime.Now >= TimeSpan.FromHours(18))
                    throw new Exception($"Перерыв стоит слишком далеко по времени, более чем через 18 часов.");
                return date;
            }
            else
            {
                throw new Exception($"Не удалось преобразовать {(time.Length > 50 ? "строку" : $"\"{time}\"")} в формат времени," +
                    $" проверьте правильность ввода сообщения.");
            }
        }
    }
}
