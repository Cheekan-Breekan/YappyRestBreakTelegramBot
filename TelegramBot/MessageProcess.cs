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
        public string Message { get; set; }
        //public string LastMessage { get; set; }
        public List<UserMessageLine> MessageLines { get; set; } = new();
        public void StartProcess(string messageText)
        {
            Message = messageText.Trim();
            try
            {
                foreach (var line in Message.Split($"\n"))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var trimmedLine = line.Trim();
                    var date = ConvertToDate(trimmedLine.Split(' ').First());
                    var info = string.Join(" ", trimmedLine.Split(' ').Skip(1));
                    InsertInList(date, info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void InsertInList(DateTime date, string info)
        {
            DeleteOldDates();
            if (MessageLines.Count == 0)
            {
                MessageLines.Add(new UserMessageLine(date, info));
                return;
            }
            if (MessageLines.First().DinnerDate > date)
            {
                MessageLines.Insert(0, new UserMessageLine(date, info));
                return;
            }
            foreach (var line in MessageLines.ToList())
            {
                if (line.DinnerDate > date)
                {
                    var index = MessageLines.IndexOf(line);
                    MessageLines.Insert(index, new UserMessageLine(date, info));
                    break;
                }
                else if (line.DinnerDate <= date && MessageLines.IndexOf(line) == MessageLines.Count - 1)
                {
                    MessageLines.Add(new UserMessageLine(date, info));
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
            foreach (var line in MessageLines.ToList())
            {
                if (line.ToString() == needToDelete)
                    MessageLines.Remove(line);
            }
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
