using System;
using Avalonia.Controls;
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