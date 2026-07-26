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

        GlobalModel.GravityConeClient.OnEvent += OnClientEvent;
        Unloaded += (_, _) =>
        {
            GlobalModel.GravityConeClient.OnEvent -= OnClientEvent;
        };
        OnClientEvent(null, null);
    }

    public async void OnClientEvent(object? obj, CliEvent? e)
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
                var status = await GlobalModel.GravityConeClient.GetRoomStatusAsync();
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

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Host)
                GlobalModel.GravityConeClient?.StopRoomAsync();
            if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Guest)
                GlobalModel.GravityConeClient?.LeaveRoomAsync();
        }
        catch
        {
        }

        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
        GlobalModel.CurrentRoomState = null;
    }

    private void RoomCodeBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            clipboard.SetTextAsync(GlobalModel.CurrentRoomState?.RoomCode);
            GlobalModel.MainWindow.Notice.AddNotice(new ()
            {
                Title = "剪切板",
                Message = "已将联机码复制至剪切板",
                NoticeType = NoticeType.Info
            });
        }
    }
}