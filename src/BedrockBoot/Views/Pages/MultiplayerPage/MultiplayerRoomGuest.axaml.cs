using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerRoomGuest : UserControl
{
    public MultiplayerRoomGuest()
    {
        InitializeComponent();

        GlobalModel.PaperConnectCore.OnPlayerInfoUpdated = (list =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                PlayerList.Children.Clear();
                list.ForEach(p =>
                {
                    Console.WriteLine($"接收到万家心跳：{p.PlayerName}");
                    PlayerList.Children.Add(new SettingCard()
                    {
                        Header = p.PlayerName,
                        Description = p.ClientId
                    });
                });
            });
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