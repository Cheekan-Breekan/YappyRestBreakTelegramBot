namespace TelegramBot;
public static class FileOperations
{
    public static List<string> ReadChatsId()
    {
        var list = new List<string>();
		try
		{
			list.AddRange(File.ReadAllLines($"ChatsId{Path.DirectorySeparatorChar}id.txt").ToList());
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
		return list;
    }
	public static void WriteChatId(long chatId)
	{
		Directory.CreateDirectory("ChatsId");
		File.AppendAllText($"ChatsId{Path.DirectorySeparatorChar}id.txt", $"{Environment.NewLine}{chatId}");
	}
}
