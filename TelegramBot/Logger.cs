namespace TelegramBot
{
    internal class Logger
    {
        private const string FolderName = "Logs";
        private DateTime Date { get; set; }
        private string FilePath { get; set; }
        public Logger()
        {
            CreateNewLogFile();
        }
        private void CreateNewLogFile()
        {
            Date = DateTime.Now;
            Directory.CreateDirectory(FolderName);
            FilePath = @$"{FolderName}{Path.DirectorySeparatorChar}log_{Date:dd.MM.yyyy_HH.mm.ss}.log";
        }
        public void LogMessage(string message, string userID)
        {
            var data = $"Сообщение от {userID} в {DateTime.Now}{Environment.NewLine}{message}{Environment.NewLine}";
            if (DateTime.Now - Date >= TimeSpan.FromHours(24))
                CreateNewLogFile();
            File.AppendAllText(FilePath, data);
        }
    }
}
