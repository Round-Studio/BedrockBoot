/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Helper;

public static class GitHubErrorDialog
{
    /// <summary>
    ///     展示 GitHub 相关错误的统一弹窗。
    ///     <paramref name="secondaryButtonText" /> 与 <paramref name="secondaryAction" />
    ///     都提供时会出现第二个按钮，用于「报错但还有一条退路」的场景（如强制下载）。
    /// </summary>
    public static async Task ShowAsync(Exception exception, string? title = null, string? closeButtonText = null,
        string? secondaryButtonText = null, Action? secondaryAction = null)
    {
        var error = GitHubHelper.HandleException(exception);

        // 认证类错误的提示本身已经说明了该做什么，再叠一句"请检查网络"只会误导
        var isAuthIssue = error.Type is GitHubErrorType.Unauthorized or GitHubErrorType.ReauthRequired;

        var dialog = new DialogInfo
        {
            Title = title ?? I18nManager.Instance["GitHub.Error.Title"],
            Content = isAuthIssue
                ? error.GetLocalizedMessage()
                : $"{I18nManager.Instance["GitHub.Error.Prefix"]}\n{error.GetLocalizedMessage()}",
            CloseButtonText = closeButtonText ?? I18nManager.Instance["Shared.Action.Confirm"]
        };

        if (!string.IsNullOrEmpty(secondaryButtonText) && secondaryAction != null)
        {
            dialog.SecondaryButtonText = secondaryButtonText;
            dialog.SecondaryAction = secondaryAction;
        }

        await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(dialog));
    }
}
