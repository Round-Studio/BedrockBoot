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
using Avalonia.Controls;
using Avalonia.Interactivity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

/// <summary>
///     登录前选择 GitHub 授权范围：完整授权（repo，可下载 Actions 产物）或仅登录
///     （user:email，只绕过 API 限流）。完整授权必须先勾选用途承诺才能点击。
/// </summary>
public partial class DialogGitHubLoginScopeContent : UserControl
{
    public DialogGitHubLoginScopeContent()
    {
        InitializeComponent();
    }

    /// <summary>用户确认后回调；true = 完整授权，false = 仅登录</summary>
    public event Action<bool>? Confirmed;

    private void AckCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        FullAccessBtn.IsEnabled = AckCheck.IsChecked == true;
    }

    private void FullAccessBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Confirm(true);
    }

    private void LoginOnlyBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Confirm(false);
    }

    private void Confirm(bool fullAccess)
    {
        DialogHost.Close();
        Confirmed?.Invoke(fullAccess);
    }
}
