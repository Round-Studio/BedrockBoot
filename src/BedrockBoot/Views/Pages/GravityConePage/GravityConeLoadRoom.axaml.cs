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

        Task.Run(async () =>
        {
            Thread.Sleep(500);
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
                try
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
                catch { }
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
        GlobalModel.CurrentRoomState = null;
    }
}