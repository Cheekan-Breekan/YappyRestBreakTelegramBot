namespace TelegramBot;
public static class FileOperations
{
    public static List<string> ReadId(string fileName)
    {
        var list = new List<string>();
		try
		{
			list.AddRange(File.ReadAllLines($"{AppDomain.CurrentDomain.BaseDirectory}{Path.DirectorySeparatorChar}{fileName}.json").ToList());
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
		File.Copy(backup + "appsettings.json", appPath + "appsettings.json", true);
		File.Copy(backup + "chats.json", appPath + "chats.json", true);
        File.Copy(backup + "access.json", appPath + "access.json", true);
    }
}