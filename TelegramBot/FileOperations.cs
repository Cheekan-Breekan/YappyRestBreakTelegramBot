namespace TelegramBot;
public static class FileOperations
{
    public const string FileNameChats = "chats.txt";
    public const string FileNameAccess = "access.txt";
    public const string FileNameSettings = "appsettings.json";
	private const string BackupFolder = "BackupSettings";
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
		var backup = appPath + BackupFolder + Path.DirectorySeparatorChar;
		File.Copy(backup + FileNameSettings, appPath + FileNameSettings, true);
		File.Copy(backup + FileNameChats, appPath + FileNameChats, true);
        File.Copy(backup + FileNameAccess, appPath + FileNameAccess, true);
    }
	public static string[] CreateFileNamesArray() =>
    [
        FileNameChats,
        FileNameAccess,
        FileNameSettings
    ];
	public static bool FileContainsValue(string value, bool isAdminCheck)
	{
		var fileName = isAdminCheck ? FileNameAccess : FileNameChats;
		if (value is null || fileName is null) { return false; }
		return ReadId(fileName).Any(s => s == value);
	}
    public static bool CheckForFileName(string fileName) => CreateFileNamesArray().Any(s => s == fileName);
}