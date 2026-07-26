using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.GravityCone.Entry.Result;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeRoom : UserControl
{
    public GravityConeRoom()
    {
        InitializeComponent();

        var client = GlobalModel.GravityConeClient;
        if (client != null)
        {
            client.OnEvent += OnClientEvent;
            Unloaded += (_, _) =>
            {
                client.OnEvent -= OnClientEvent;
            };
        }

        OnClientEvent(null, null);
    }

    public async void OnClientEvent(object? obj, CliEvent? e)
    {
        // async void：任何未捕获异常都会直接终止进程，必须整体兜底
        try
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                if(RoomCodeBtn == null)
                    return;
                
                RoomCodeBtn.Content = GlobalModel.CurrentRoomState?.RoomCode;
            });
            if (PlayersList != null)
            {
                RoomJoinResult? result = null;
                if (e != null)
                {
                    var even = e;
                    if (even.Event == "room.player_joined" ||
                        even.Event == "room.player_left" ||
                        even.Event == "paperconnect.room.info")
                    {
                        result = even.Data.Deserialize<RoomJoinResult>();
                    }
                }
                else
                {
                    var client = GlobalModel.GravityConeClient;
                    if (client == null || !client.IsRunning) return;

                    var status = await client.GetRoomStatusAsync();
                    result = status.Data.Deserialize<RoomJoinResult>();
                }

                if (result == null) return;
                if(result.Players == null) return;

                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    PlayersList.Children.Clear();
                    result.Players.ToList().ForEach(x => { PlayersList.Children.Add(new PlayerItem(x)); });
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"刷新房间玩家列表失败: {ex.Message}");
        }
    }

    private async void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var client = GlobalModel.GravityConeClient;
            if (client != null)
            {
                if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Host)
                    await client.StopRoomAsync();
                if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Guest)
                    await client.LeaveRoomAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"关闭/退出房间失败: {ex.Message}");
        }

        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
        GlobalModel.CurrentRoomState = null;
    }

    private void RoomCodeBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var roomCode = GlobalModel.CurrentRoomState?.RoomCode;
        if (string.IsNullOrEmpty(roomCode)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            clipboard.SetTextAsync(roomCode);
            GlobalModel.MainWindow.Notice.AddNotice(new ()
            {
                Title = "剪切板",
                Message = "已将联机码复制至剪切板",
                NoticeType = NoticeType.Info
            });
        }
    }
}