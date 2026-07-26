using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeLoadRoom : UserControl
{
    private readonly RoomType _roomType;
    private readonly string? _roomCode;

    public GravityConeLoadRoom()
    {
        InitializeComponent();
    }

    public GravityConeLoadRoom(RoomType roomType, string? roomCode = null) : this()
    {
        _roomType = roomType;
        _roomCode = roomCode;

        if (string.IsNullOrEmpty(GlobalModel.XboxUserInfo.Gamertag))
        {
            DialogHost.Show(new()
            {
                Title = "出现错误",
                Content = "Xbox 未登录，请尝试进入游戏后登录 Xbox 账户后重试。",
                CloseButtonText = "确定",
                CloseAction = () => { MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot()); }
            });
            return;
        }

        Task.Run(async () =>
        {
            Thread.Sleep(500);
            try
            {
                if (roomType == RoomType.Host)
                {
                    var room = await GlobalModel.GravityConeClient?.CreatePaperConnectRoomAsync(GlobalModel.XboxUserInfo
                        .Gamertag)!;
                    GlobalModel.CurrentRoomState = new()
                    {
                        RoomCode = room.Code,
                        RoomType = RoomType.Host
                    };
                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoom());
                    });
                }
                else
                {
                    var room = await GlobalModel.GravityConeClient?.JoinRoomAsync(_roomCode,
                        GlobalModel.XboxUserInfo.Gamertag)!;
                    GlobalModel.CurrentRoomState = new()
                    {
                        RoomCode = room.RoomCode,
                        RoomType = RoomType.Guest
                    };
                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoom());
                    });
                }
            }catch{ }
        });
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
}