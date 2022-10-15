using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class MessageProcess
    {
        const int ThirtyMinutes = 30;
        const int TenMinutes = 10;
        const int ZeroMinutes = 0;
        public string Message { get; private set; }
        public List<UserMessageLine> MessageLines { get; private set; } = new();
        public bool IsErrorDetected { get; private set; }
        public string ErrorMessage { get; private set; }
        public void StartProcess(string messageText)
        {
            IsErrorDetected = false;
            Message = messageText.Trim();
            try
            {
                foreach (var line in Message.Split($"\n"))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var splittedLine = line.Trim().Split(' ');
                    var date = ConvertToDate(splittedLine[0]);
                    var time = int.Parse(splittedLine.Last());
                    var name = line.Replace(splittedLine.First(), "").Replace(splittedLine.Last(), "").Trim();
                    if (CheckForRightDinnerTime(date.Minute, time) && CheckForFreeSlots(date, time))
                        InsertInList(date, name, time);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                IsErrorDetected = true;
                ErrorMessage = "Неправильный ввод данных. Список перерывов не изменён! Используйте команду help для помощи.";
            }
        }

        private bool CheckForRightDinnerTime(int dateMinutes, int time)
        {
            if (time == TenMinutes && (dateMinutes % TenMinutes == 0))
                return true;
            if (time == ThirtyMinutes && (ZeroMinutes == dateMinutes || ThirtyMinutes == dateMinutes))
                return true;
            IsErrorDetected = true;
            ErrorMessage = $"Неправильно проставлено время перерыва!{Environment.NewLine}Перерывы могут быть либо 10 минут, либо 30. Обеды допустимы только в :00 минут и в :30 минут," +
                $" а десятиминутки в любую минуту, оканчивающуюся на 0.{Environment.NewLine}Используйте команду help для помощи.";
            return false;
        }
        private bool CheckForFreeSlots(DateTime date, int time)
        {
            var counter = 0;
            var limit = (DateTime.Now.Hour <= 22 && DateTime.Now.Hour >= 6) ? 7 : 3;
            foreach (var line in MessageLines)
            {
                if (line.DinnerDate == date && line.Minutes == time)
                    counter++;
            }
            if (counter >= 7)
            {
                var tenOrThirty = time == 10 ? "десятиминутных перерыва(-ов)" : "обеда(-ов)";
                IsErrorDetected = true;
                ErrorMessage = $"В {date:h:m} уже {limit} {tenOrThirty}. Выберите другое время. Используйте команду help для помощи.";
                return false;
            }
            return true;
        }

        private void InsertInList(DateTime date, string info, int time)
        {
            DeleteOldDates();
            if (MessageLines.Count == 0)
            {
                MessageLines.Add(new UserMessageLine(date, info, time));
                return;
            }
            if (MessageLines.First().DinnerDate > date)
            {
                MessageLines.Insert(0, new UserMessageLine(date, info, time));
                return;
            }
            foreach (var line in MessageLines.ToList())
            {
                if (line.DinnerDate > date)
                {
                    var index = MessageLines.IndexOf(line);
                    MessageLines.Insert(index, new UserMessageLine(date, info, time));
                    break;
                }
                else if (line.DinnerDate <= date && MessageLines.IndexOf(line) == MessageLines.Count - 1)
                {
                    MessageLines.Add(new UserMessageLine(date, info, time));
                }
            }
        }
        private void DeleteOldDates()
        {
            foreach (var line in MessageLines.ToList())
            {
                if (line.DinnerDate < DateTime.Now)
                    MessageLines.Remove(line);
            }
        }
        public string GetFullMessage()
        {
            if (IsErrorDetected)
            {
                IsErrorDetected = false;
                return ErrorMessage;
            }
            string fullMessage = String.Empty;
            foreach (var line in MessageLines)
            {
                fullMessage += line + Environment.NewLine;
            }
            Console.WriteLine(MessageLines.Count);
            return fullMessage;
        }
        public void DeleteLine(string needToDelete)
        {
            var counter = 0;
            foreach (var line in MessageLines.ToList())
            {
                if (line.ToString() == needToDelete)
                {
                    MessageLines.Remove(line);
                    counter++;
                }
            }
            if (counter == 0)
            {
                ErrorMessage = "Нет совпадения в списке с введенным перерывом для удаления! Используйте команду help для помощи.";
                IsErrorDetected = true;
            }
            DeleteOldDates();
        }
        public void ClearList()
        {
            MessageLines.Clear();
        }
        private DateTime ConvertToDate(string time)
        {
            var date = DateTime.ParseExact(time, "H:m", CultureInfo.CurrentCulture);
            if (date < DateTime.Now)
                return date + TimeSpan.FromHours(24);
            return date;
        }
    }
}
