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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using PaperConnect.Core.Enum;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerRoot : UserControl
{
    public MultiplayerRoot()
    {
        InitializeComponent();
    }

    private void CreateRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GlobalModel.XboxUserInfo == null)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new()
            {
                Title = "Xbox 未登录",
                Message = "无法获取 Xbox 用户，请登录 Xbox 账户后重试",
                NoticeType = NoticeType.Error
            });
            return;
        }

        GlobalModel.PaperConnectCore = new PaperConnectCore
        {
            EasyTierCliPath = PathsList.EasyTierCliPath,
            EasyTierPath = PathsList.EasyTierCorePath,
            ClientPlayer = GlobalModel.XboxUserInfo.Gamertag,
            GamePort = 7551
        };
        Task.Run(() => GlobalModel.PaperConnectCore.Initialize(CoreType.Server, GlobalModel.ETPublicServer));
        Dispatcher.UIThread.Invoke(() => MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomHost()));
    }

    private void LinkRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GlobalModel.XboxUserInfo == null)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new()
            {
                Title = "Xbox 未登录",
                Message = "无法获取 Xbox 用户，请登录 Xbox 账户后重试",
                NoticeType = NoticeType.Error
            });
            return;
        }

        var dialog = new DialogMultiplayerLinkRoomContent();
        DialogHost.Show(new DialogInfo
        {
            Title = "连接房间",
            Content = dialog,
            CloseButtonText = "加入",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                var roomCode = dialog.RoomCode;

                if (string.IsNullOrEmpty(roomCode))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new()
                    {
                        Title = "不得为空",
                        Message = "联机码不得为空",
                        NoticeType = NoticeType.Error
                    });
                    return;
                }

                GlobalModel.PaperConnectCore = new PaperConnectCore
                {
                    EasyTierCliPath = PathsList.EasyTierCliPath,
                    EasyTierPath = PathsList.EasyTierCorePath,
                    ClientPlayer = GlobalModel.XboxUserInfo.Gamertag,
                    RoomCode = roomCode
                };
                GlobalModel.PaperConnectCore.LinkSuccess = () =>
                {
                    Dispatcher.UIThread.Invoke(() => DialogHost.Close());
                };
                Task.Run(() =>
                {
                    try
                    {
                        GlobalModel.PaperConnectCore.Initialize(CoreType.Client, GlobalModel.ETPublicServer);
                    }
                    catch (Exception ex)
                    {
                        // 联机码非法等情况 Initialize 会抛异常，
                        // 必须关闭"连接房间中"对话框并回到联机首页，否则 UI 永久卡死
                        Console.WriteLine($@"连接房间失败: {ex}");
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            DialogHost.Close();
                            MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoot());
                            GlobalModel.MainWindow.Notice.AddNotice(new()
                            {
                                Title = "连接房间失败",
                                Message = ex.Message,
                                NoticeType = NoticeType.Error
                            });
                        });
                    }
                });
                Dispatcher.UIThread.Invoke(() =>
                    MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomGuest()));
                DialogHost.Show(new DialogInfo
                {
                    Title = "连接房间中...",
                    Content = "正在连接房间..."
                });
            }
        });
    }
}