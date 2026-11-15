using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Helper;

public static class GitHubErrorDialog
{
    public static async Task ShowAsync(Exception exception, string title = "GitHub 请求失败", string closeButtonText = "确定")
    {
        var error = GitHubHelper.HandleException(exception);
        var dialog = new DialogInfo
        {
            Title = title,
            Content = $"无法从 GitHub 获取信息，请检查网络后重试。\n{error.GetLocalizedMessage()}",
            CloseButtonText = closeButtonText
        };

        await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(dialog));
    }
}
