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
