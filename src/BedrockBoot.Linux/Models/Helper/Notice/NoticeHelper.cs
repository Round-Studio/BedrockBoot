namespace BedrockBoot.Models.Helper.Notice;

public class NoticeHelper
{
    public static void SentNotice(string title, string message)
    {
        Console.WriteLine("在 Linux 中尚不支持使用系统消息模块");
    }
}