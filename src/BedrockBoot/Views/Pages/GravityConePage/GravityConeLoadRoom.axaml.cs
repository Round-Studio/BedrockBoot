using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.GravityCone;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeLoadRoom : UserControl
{
    private readonly RoomType _roomType;
    private readonly string? _roomCode;

    /// <summary>用户是否已点击退出。为 true 时进行中的创建/加入完成后不得再导航进房间。</summary>
    private volatile bool _closed;

    public GravityConeLoadRoom()
    {
        InitializeComponent();
    }

    public GravityConeLoadRoom(RoomType roomType, string? roomCode = null) : this()
    {
        _roomType = roomType;
        _roomCode = roomCode;

        if (string.IsNullOrEmpty(GlobalModel.XboxUserInfo?.Gamertag))
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
            await Task.Delay(500);
            try
            {
                var client = GlobalModel.GravityConeClient;
                if (client == null || !client.IsRunning)
                    throw new InvalidOperationException("联机组件未运行，请返回重新进入联机页面。");

                if (roomType == RoomType.Host)
                {
                    var room = await client.CreatePaperConnectRoomAsync(GlobalModel.XboxUserInfo.Gamertag);

                    // 用户已点退出：房间已在 CLI 侧创建，需要关掉，且不再导航
                    if (_closed)
                    {
                        try { await client.StopRoomAsync(); } catch { }
                        return;
                    }

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
                    var room = await client.JoinRoomAsync(_roomCode, GlobalModel.XboxUserInfo.Gamertag);

                    if (_closed)
                    {
                        try { await client.LeaveRoomAsync(); } catch { }
                        return;
                    }

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
            }
            catch (CliErrorException)
            {
                // CLI 业务错误：GravityConeInit 的全局 OnResponse 处理器
                // 已负责弹窗与导航，这里不再重复提示
            }
            catch (Exception ex)
            {
                // 此前这里是空 catch：任何失败（超时、组件未运行等）都会让用户
                // 永远停留在转圈的加载页。改为提示并返回大厅。
                // CLI 返回的业务错误响应已由 GravityConeInit 的 OnResponse 全局处理器
                // 负责弹窗与导航，这里只兜底本地异常。
                Console.WriteLine($@"创建/加入房间失败: {ex}");
                if (_closed) return;

                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                    DialogHost.Show(new()
                    {
                        Title = _roomType == RoomType.Host ? "创建房间失败" : "加入房间失败",
                        Content = ex.Message,
                        CloseButtonText = "确定"
                    });
                });
            }
        });
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _closed = true;

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