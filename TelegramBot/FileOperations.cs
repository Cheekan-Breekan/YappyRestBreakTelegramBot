namespace TelegramBot;
public static class FileOperations
{
	public const string chatsFileName = "chats.json";
    public const string accessFileName = "access.json";
    public const string settingsFileName = "appsettings.json";
    public static List<string> ReadId(string fileName)
    {
        var list = new List<string>();
		try
		{
			list.AddRange(File.ReadAllLines($"{AppDomain.CurrentDomain.BaseDirectory}{Path.DirectorySeparatorChar}{fileName}").ToList());
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, $"Ошибка! Не удалось прочесть файл {fileName}");
		}
		return list;
    }
	public static void RestoreDefault()
	{
		var appPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar;
		var backup = appPath + "backup" + Path.DirectorySeparatorChar;
		File.Copy(backup + settingsFileName, appPath + settingsFileName, true);
		File.Copy(backup + chatsFileName, appPath + chatsFileName, true);
        File.Copy(backup + accessFileName, appPath + accessFileName, true);
    }
}