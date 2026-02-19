using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
        GlobalModel.PaperConnectCore = new PaperConnectCore()
        {
            EasyTierCliPath = PathsList.EasyTierCliPath,
            EasyTierPath = PathsList.EasyTierCorePath,
            ClientPlayer = "Host",
            GamePort = 7551
        };
        Task.Run(() => GlobalModel.PaperConnectCore.Initialize(CoreType.Server, GlobalModel.ETPublicServer));
        Dispatcher.UIThread.Invoke(() => MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomHost()));
    }

    private void LinkRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogMultiplayerLinkRoomContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "连接房间",
            Content = dialog,
            CloseButtonText = "加入",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                var playerName = dialog.PlayerName;
                var roomCode = dialog.RoomCode;
                
                GlobalModel.PaperConnectCore = new PaperConnectCore()
                {
                    EasyTierCliPath = PathsList.EasyTierCliPath,
                    EasyTierPath = PathsList.EasyTierCorePath,
                    ClientPlayer = playerName,
                    RoomCode = roomCode
                };
                GlobalModel.PaperConnectCore.LinkSuccess = () =>
                {
                    DialogHost.Close();
                    Dispatcher.UIThread.Invoke(() =>
                        MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomHost()));
                };
                Task.Run(() => GlobalModel.PaperConnectCore.Initialize(CoreType.Client, GlobalModel.ETPublicServer));
                DialogHost.Show(new()
                {
                    Title = "连接房间中...",
                    Content = "正在连接房间..."
                });
            }
        });
    }
}