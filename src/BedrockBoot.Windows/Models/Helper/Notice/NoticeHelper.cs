using Microsoft.Toolkit.Uwp.Notifications;

namespace BedrockBoot.Models.Helper.Notice;

public class NoticeHelper
{
    public static void SentNotice(string title, string message)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .Show();
    }
}