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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogLoginMsAccountContent : UserControl
{
    private string _code;

    public DialogLoginMsAccountContent()
    {
        InitializeComponent();
    }

    public void SetCopyCode(string code, string link)
    {
        LinkBox.IsVisible = true;
        LinkBtn.NavigateUri = new Uri(link);

        CodeCopyBtn.Content = code;
        CodeCopyBtn.IsEnabled = true;
        _code = code;
    }

    private void CodeCopyBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_code)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            clipboard.SetTextAsync(_code);
        }
    }
}