using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.GravityCone.Entry.Result;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeRoomCreate : UserControl
{
    private string _code;
    public GravityConeRoomCreate()
    {
        InitializeComponent();
        RoomCodeCard.IsVisible = false;

        if (GlobalModel.GravityConeClient == null)
        {
            MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeInit());
            return;
        }
        if (!GlobalModel.GravityConeClient.IsRunning)
        {
            MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeInit());
            return;
        }

        GlobalModel.GravityConeClient.OnEvent += (sender, @event) =>
        {
            if (PlayersList != null)
            {
                var even = @event;
                if (even.Event == "room.player_joined" ||
                    even.Event == "room.player_left" ||
                    even.Event == "paperconnect.room.info")
                {
                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        var playerList = even.Data.Deserialize<RoomJoinResult>();
                        PlayersList.Children.Clear();
                        playerList.Players.ToList().ForEach(x =>
                        {
                            PlayersList.Children.Add(new PlayerItem(x));
                        });
                    });
                }
            }
        };

        Task.Run(async () =>
        {
            try
            {
                var room = await GlobalModel.GravityConeClient?.CreatePaperConnectRoomAsync(GlobalModel.XboxUserInfo
                    .Gamertag);
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    SearchGame.IsVisible = false;
                    RoomCodeCard.IsVisible = true;
                    RoomCodeBtn.Content = room.Code;
                    PlayersBox.IsVisible = true;
                    _code = room.Code;
                });
            }
            catch
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    if (!GlobalModel.GravityConeClient.IsRunning)
                        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeInit());
                });
            }
        });
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            GlobalModel.GravityConeClient.LeaveRoomAsync();
            GlobalModel.GravityConeClient.StopRoomAsync();
        }
        catch
        {
        }

        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
    }

    private void RoomCodeBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            clipboard.SetTextAsync(_code);
            GlobalModel.MainWindow.Notice.AddNotice(new ()
            {
                Title = "剪切板",
                Message = "已将联机码复制至剪切板",
                NoticeType = NoticeType.Info
            });
        }
    }
}