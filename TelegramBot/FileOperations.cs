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
			Console.WriteLine(ex);
		}
		return list;
    }
	public static void WriteChatId(long chatId)
	{
		File.AppendAllText($"Id{Path.DirectorySeparatorChar}chats.txt", $"{Environment.NewLine}{chatId}");
	}
}
