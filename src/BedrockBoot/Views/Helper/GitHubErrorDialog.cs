using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Helper;

public static class GitHubErrorDialog
{
    public static async Task ShowAsync(Exception exception, string? title = null, string? closeButtonText = null)
    {
        var error = GitHubHelper.HandleException(exception);
        var dialog = new DialogInfo
        {
            Title = title ?? I18nManager.Instance["GitHub.Error.Title"],
            Content = $"{I18nManager.Instance["GitHub.Error.Prefix"]}\n{error.GetLocalizedMessage()}",
            CloseButtonText = closeButtonText ?? I18nManager.Instance["Shared.Action.Confirm"]
        };

        await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(dialog));
    }
}
