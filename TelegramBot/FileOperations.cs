namespace TelegramBot;
public static class FileOperations
{
	public const string fileNameChats = "chats.json";
    public const string fileNameAccess = "access.json";
    public const string fileNameSettings = "appsettings.json";
    private static List<string> ReadId(string fileName)
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
		File.Copy(backup + fileNameSettings, appPath + fileNameSettings, true);
		File.Copy(backup + fileNameChats, appPath + fileNameChats, true);
        File.Copy(backup + fileNameAccess, appPath + fileNameAccess, true);
    }
	public static string[] CreateFileNamesArray() => new string[]
    {
        fileNameChats,
        fileNameAccess,
        fileNameSettings
    };
	public static bool FileContainsValue(string value, string fileName = fileNameAccess)
	{
		if (value is null || fileName is null) { return false; }
		return ReadId(fileName).Contains(value);
	}
    public static bool CheckForFileName(string fileName) => CreateFileNamesArray().Any(s => s == fileName);
	public static List<string> LoadChatIds() => ReadId(fileNameChats);
}