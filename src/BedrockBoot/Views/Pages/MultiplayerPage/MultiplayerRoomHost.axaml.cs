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
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerRoomHost : UserControl
{
    public MultiplayerRoomHost()
    {
        InitializeComponent();

        GlobalModel.PaperConnectCore.OnPlayerInfoUpdated = list =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                PlayerList.Children.Clear();
                list.ForEach(p =>
                {
                    Console.WriteLine($@"接收到心跳：{p.PlayerName}");
                    PlayerList.Children.Add(new PlayerItem(p));
                });
            });
        };
    }

    private void CopyCode_OnClick(object? sender, RoutedEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(GlobalModel.PaperConnectCore.RoomCode);
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "联机大厅",
            Message = "联机码已复制到剪切板"
        });
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.PaperConnectCore.Stop();
        GlobalModel.PaperConnectCore = null;

        Dispatcher.UIThread.Invoke(() =>
            MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoot()));
    }
}