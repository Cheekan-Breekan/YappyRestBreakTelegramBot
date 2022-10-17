using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            ErrorMessage = String.Empty;
            IsErrorDetected = false;
            Message = messageText.Trim();
            //bool tempLengthCheck = Message.Contains("insert") ? true : false;
            var tempLengthCheck = false;
            var firstLine = false;
            if (Message.Contains("insert"))
            {
                tempLengthCheck = true;
                firstLine = true;
            }
            try
            {
                DateTime date = new DateTime();
                int time = 0;
                string name = string.Empty;
                foreach (var line in Message.Split($"\n"))
                {
                    if (string.IsNullOrWhiteSpace(line) || (tempLengthCheck && firstLine))
                    {
                        firstLine = false;
                        continue;
                    }    
                    var splittedLine = line.Trim().Split(' ');
                    try
                    {
                        date = ConvertToDate(splittedLine[0]);
                        time = int.Parse(splittedLine.Last());
                        name = line.Replace(splittedLine.First(), "").Replace(splittedLine.Last(), "").Trim();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        IsErrorDetected = true;
                        ErrorMessage += $"Грамматически неправильный перерыв в {date:H:mm}! Измените этот конкретный перерыв и отправьте его заново. " +
                            $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                        continue;
                    }
                    if (Message.Split($"\n").Count() > 7 && !tempLengthCheck)
                    {
                        IsErrorDetected = true;
                        ErrorMessage = "Неправильный ввод данных. Список перерывов не изменён! Вы вносите слишком много перерывов, добавляйте лишь свои!";
                        return;
                    }
                    if (CheckForRightDinnerTime(date, time) && CheckForFreeSlots(date, time))
                    {
                        InsertInList(date, name, time);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                IsErrorDetected = true;
                ErrorMessage = "Неправильный ввод данных. Список перерывов не изменён!";
            }
        }

        private bool CheckForRightDinnerTime(DateTime date, int time)
        {
            int dateMinutes = date.Minute;
            if (time == TenMinutes && (dateMinutes % TenMinutes == 0))
                return true;
            if (time == ThirtyMinutes && (ZeroMinutes == dateMinutes || ThirtyMinutes == dateMinutes))
                return true;
            IsErrorDetected = true;
            ErrorMessage += $"Неправильный перерыв в {date:H:mm}! Измените этот конкретный перерыв и отправьте его заново. Правильные перерывы записаны!{Environment.NewLine}Перерывы могут быть либо 10 минут, либо 30. Обеды допустимы только в :00 минут и в :30 минут," +
                $" а десятиминутки в любую минуту, оканчивающуюся на 0.{Environment.NewLine}{Environment.NewLine}";
            return false;
        }
        private bool CheckForFreeSlots(DateTime date, int time)
        {
            var counter = 0;
            //var limit = (DateTime.Now.Hour <= 22 && DateTime.Now.Hour >= 6) ? 7 : 3;
            var limit = (date.Hour < 22 && date.Hour >= 6) ? 7 : 5;
            foreach (var line in MessageLines)
            {
                if (line.DinnerDate == date && line.Minutes == time)
                    counter++;
            }
            if (counter >= limit)
            {
                var tenOrThirty = time == 10 ? "десятиминутных перерыва(-ов)" : "обеда(-ов)";
                IsErrorDetected = true;
                ErrorMessage += $"В {date:H:mm} уже {limit} {tenOrThirty}. Выберите для этого конкретного перерыва другое время и отправьте его заново. Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
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
                return ErrorMessage += " Используйте команду help для помощи.";
            }
            string fullMessage = String.Empty;
            foreach (var line in MessageLines)
            {
                fullMessage += line + Environment.NewLine;
            }
            //Console.WriteLine(MessageLines.Count);
            return fullMessage;
        }
        public void DeleteLine(string needToDelete, bool IsWorkaround = false)
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
                if (!IsWorkaround)
                    ErrorMessage = "Нет совпадения в списке с введенным перерывом для удаления!";
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
