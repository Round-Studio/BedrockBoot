using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerRoomGuest : UserControl
{
    public MultiplayerRoomGuest()
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

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.PaperConnectCore.Stop(true);
        GlobalModel.PaperConnectCore = null;

        Dispatcher.UIThread.Invoke(() =>
            MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoot()));
    }
}