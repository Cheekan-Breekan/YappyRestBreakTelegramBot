using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
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
            var tempLengthCheck = false;
            var firstLine = false;
            var counter = 0;
            if (Message.Contains("insert"))
            {
                tempLengthCheck = true;
                firstLine = true;
            }
            try
            {
                foreach (var line in Message.Split($"\n"))
                {
                    DateTime date = new DateTime();
                    int time = 0;
                    string name = null;
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
                        name = ConvertToName(line, splittedLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        IsErrorDetected = true;
                        if (counter == 0)
                        {
                            ErrorMessage += $"Неверный ввод данных! Список перерывов никак не изменён.";
                            return;
                        }
                        if (date == default)
                            ErrorMessage += $"Присутствует грамматически неправильный перерыв {(line.Length > 50 ? "с неизвестным временем" : $"({line})")}!" +
                                $" Измените этот конкретный перерыв и отправьте его заново. " +
                                $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                        else
                            ErrorMessage += $"Грамматически неправильный перерыв {(line.Length > 50 ? $"в {date:HH:mm}" : $"({line})")}! Измените этот конкретный перерыв и отправьте его заново. " +
                                $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                        continue;
                    }
                    if (Message.Split($"\n").Count() > 7 && !tempLengthCheck)
                    {
                        IsErrorDetected = true;
                        ErrorMessage = "Неправильный ввод данных. Список перерывов никак не изменён! Вы вносите слишком много перерывов, добавляйте лишь свои!";
                        return;
                    }
                    if (CheckForRightDinnerTime(date, time) && CheckForFreeSlotsAndDub(date, time))
                    {
                        InsertInList(date, name, time);
                    }
                    counter++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                IsErrorDetected = true;
                ErrorMessage = "Неправильный ввод данных! " + ex;
            }
        }
        private bool CheckForRightDinnerTime(DateTime date, int time)
        {
            var rightTime = time == TenMinutes || time == ThirtyMinutes;
            int dateMinutes = date.Minute;
            if (time == TenMinutes && (dateMinutes % TenMinutes == 0))
                return true;
            if (time == ThirtyMinutes && (ZeroMinutes == dateMinutes || ThirtyMinutes == dateMinutes))
                return true;
            IsErrorDetected = true;
            ErrorMessage += $"Неправильный перерыв в {date:H:mm}! Измените этот конкретный перерыв и отправьте его заново. " +
                $"Правильные перерывы записаны!{Environment.NewLine}{(rightTime ? "" : "Перерывы могут быть либо 10 минут, либо 30. ")}" +
                $"{(time == ThirtyMinutes ? "Обеды допустимы только в :00 минут и в :30 минут." : "")}" +
                $"{(time == TenMinutes ? "Десятиминутки допустимы в любую минуту, кратную 10." : "")}{Environment.NewLine}{Environment.NewLine}";
            return false;
        }
        private bool CheckForFreeSlotsAndDub(DateTime date, int time)
        {
            var counter = 0;
            var dinnerLimit = (date.Hour < 22 && date.Hour >= 6) ? 10 : 5;
            var breakLimit = (date.Hour < 22 && date.Hour >= 6) ? 7 : 5;
            var isTenMinutes = time == TenMinutes ? true : false;
            foreach (var line in MessageLines)
            {
                if (line.DinnerDate == date && line.Minutes == time)
                    counter++;
            }
            if (isTenMinutes && counter >= breakLimit)
            {
                IsErrorDetected = true;
                ErrorMessage += $"В {date:H:mm} уже {breakLimit} десятиминуток. Выберите для этого конкретного перерыва другое время и отправьте его заново. " +
                    $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                return false;
            }
            if (counter >= dinnerLimit)
            {
                IsErrorDetected = true;
                ErrorMessage += $"В {date:H:mm} уже {dinnerLimit} обедов. Выберите для этого конкретного перерыва другое время и отправьте его заново. " +
                    $"Правильные перерывы записаны!{Environment.NewLine}{Environment.NewLine}";
                return false;
            }
            return true;
        }

        private void InsertInList(DateTime date, string info, int time)
        {
            DeleteOldDates();
            var newLine = new UserMessageLine(date, info, time);
            if (MessageLines.Count == 0)
            {
                MessageLines.Add(newLine);
                return;
            }
            if (MessageLines.First().DinnerDate > date)
            {
                MessageLines.Insert(0, newLine);
                return;
            }
            foreach (var line in MessageLines.ToList())
            {
                if (line.ToString() == newLine.ToString())
                {
                    IsErrorDetected = true;
                    ErrorMessage += $"В {date:HH:mm} уже проставлен идентичный перерыв! Не дублируйте! Правильные перерывы записаны.{Environment.NewLine}{Environment.NewLine}";
                    break;
                }
                else if (line.DinnerDate > date)
                {
                    var index = MessageLines.IndexOf(line);
                    MessageLines.Insert(index, newLine);
                    break;
                }
                else if (line.DinnerDate <= date && MessageLines.IndexOf(line) == MessageLines.Count - 1)
                {
                    MessageLines.Add(newLine);
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
        private string ConvertToName(string line, string[] splittedLine)
        {
            var name = line.Replace(splittedLine.First(), "").Replace(splittedLine.Last(), "").Trim();
            if (string.IsNullOrWhiteSpace(name)) { throw new Exception("Имя/Фамилия пусты"); }
            if (name.Length > 50) { throw new Exception("Слишком длинные имя/фамилия"); }
            if (name.Any(char.IsDigit)) { throw new Exception("Имя/Фамилия содержат цифры"); }
            return name;
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
