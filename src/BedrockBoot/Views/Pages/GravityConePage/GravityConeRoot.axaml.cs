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

using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper.GravityCone;
using BedrockBoot.Views.DialogContent.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeRoot : UserControl
{
    public GravityConeRoot()
    {
        InitializeComponent();
    }

    private void CreateRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeLoadRoom(RoomType.Host));
    }

    private void LinkRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogMultiplayerLinkRoomContent();
        DialogHost.Show(new()
        {
            Title = "加入房间",
            Content = dialog,
            CloseButtonText = "加入",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.RoomCode))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new()
                    {
                        Title = "不得为空",
                        Message = "联机码不得为空",
                        NoticeType = NoticeType.Error
                    });
                    return;
                }

                MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeLoadRoom(RoomType.Guest,
                    dialog.RoomCode));
            }
        });
    }
}