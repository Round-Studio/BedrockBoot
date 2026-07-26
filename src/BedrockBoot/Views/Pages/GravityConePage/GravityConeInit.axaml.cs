using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Core.Models.Xbox;
using BedrockBoot.GravityCone;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeInit : UserControl
{
    public GravityConeInit()
    {
        InitializeComponent();
        Task.Run(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        var task1 = GetNodesAsync();
        var task2 = GetXboxUserAsync();

        await Task.WhenAll(task1, task2);

        // 任一前置任务失败时不能继续启动 CLI：
        // 节点列表为空会让打洞失败，Xbox 用户为空会让房间玩家名非法。
        // 此前这里无条件继续，失败弹窗和后台初始化会同时发生。
        if (!task1.Result || !task2.Result) return;

        await ThirdTaskAsync();
    }

    /// <summary>
    /// 返回主页。MainPage.Instance 在使用 NeoMainPage（Beta UI）时为 null，
    /// 直接访问会在错误弹窗的回调里再抛一个 NullReferenceException。
    /// </summary>
    private static void BackToMain()
    {
        if (MainPage.Instance != null)
            MainPage.Instance.SelTag.SelectedIndex = 0;
    }

    private async Task<bool> GetNodesAsync()
    {
        using var client = new HttpClient();

        try
        {
            var url = "https://et-public-node.roundstudio.top/";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var nodeList = JsonSerializer.Deserialize<List<string>>(jsonResponse);

            if (nodeList != null)
            {
                Console.WriteLine($@"成功获取到 {nodeList.Count} 个节点：");
                GlobalModel.ETPublicServer = nodeList;
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    this.LoadGetPeersBar.IsIndeterminate = false;
                    this.LoadGetPeersBar.Value = 100;
                });
                return true;
            }

            Console.WriteLine(@"反序列化结果为空。");
            throw new Exception();
        }
        catch
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                this.LoadGetPeersBar.IsIndeterminate = false;
                this.LoadGetPeersBar.Value = 0;
                DialogHost.Show(new()
                {
                    Title = "获取节点失败",
                    Content = "请尝试更换网络，然后重试",
                    CloseButtonText = "确定",
                    CloseAction = BackToMain
                });
            });
            return false;
        }
    }

    private async Task<bool> GetXboxUserAsync()
    {
        var checker = new XboxLoginStatusChecker();
        var status = await checker.GetDetailedXboxStatus();

        if (!status.IsLoggedIn ||
            status.XboxUserInfo == null ||
            string.IsNullOrEmpty(status.XboxUserInfo.Gamertag))
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                this.LoadGetPeersBar.IsIndeterminate = false;
                this.LoadGetPeersBar.Value = 0;
                DialogHost.Show(new()
                {
                    Title = "获取 Xbox 用户失败",
                    Content = "无法获取 Xbox 用户，请尝试重新登录 Xbox 账户",
                    CloseButtonText = "确定",
                    CloseAction = BackToMain
                });
            });
            return false;
        }

        GlobalModel.XboxUserInfo = status.XboxUserInfo;
        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            this.LoadGetXboxUserBar.IsIndeterminate = false;
            this.LoadGetXboxUserBar.Value = 100;
        });
        return true;
    }

    private async Task ThirdTaskAsync()
    {
        await Task.Delay(100);

        var client = new GravityConeClient();
        try
        {
            // Client 已改用 ArgumentList 传参，vendor/motd 不需要再嵌入引号
            await client.StartAsync(
                Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath,
                    "gravitycone-cli-windows-amd64.exe"),
                GlobalModel.ETPublicServer, $"BedrockBoot {GlobalModel.BodyVersion}", "BedrockBoot 联机房间",
                DialogDownloadMultiPlayerDependenceContent.GravityConeExePath);
        }
        catch (Exception ex)
        {
            // 启动失败时必须释放并保持 GlobalModel.GravityConeClient 为 null：
            // 否则 MainGravityConePage 会把残留的半初始化客户端当作可用状态，
            // 之后所有请求都会失败且用户永远停在加载页。
            Console.WriteLine($@"GravityCone CLI 启动失败: {ex}");
            try { client.Dispose(); } catch { }

            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                LoadInitBar.IsIndeterminate = false;
                LoadInitBar.Value = 0;
                DialogHost.Show(new()
                {
                    Title = "联机组件启动失败",
                    Content = $"无法启动联机组件，请重试或联系开发者。\n{ex.Message}",
                    CloseButtonText = "确定",
                    CloseAction = BackToMain
                });
            });
            return;
        }

        GlobalModel.GravityConeClient = client;

        GlobalModel.GravityConeClient.OnEvent += (sender, eventArgs) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                if (eventArgs.Event == "paperconnect.connection.error")
                {
                    GlobalModel.CurrentRoomState = null;
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                    DialogHost.Show(new()
                    {
                        Title = "出现错误",
                        Content = "当前无法与游戏进行通讯，请联系开发者并向开发者提供信息",
                        CloseButtonText = "确定"
                    });
                }
                if (eventArgs.Event == "paperconnect.connection.port_busy")
                {
                    GlobalModel.CurrentRoomState = null;
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                    DialogHost.Show(new()
                    {
                        Title = "出现错误",
                        Content = "目标端口被游戏占用，请确保当前后台无游戏运行。\n" +
                                  "成功进入房间后即可启动游戏",
                        CloseButtonText = "确定"
                    });
                }
                if (eventArgs.Event == "paperconnect.connection.disconnected" ||
                    eventArgs.Event == "paperconnect.connection.closed")
                {
                    // 必须清除房间状态：否则下次进入联机页时，
                    // MainGravityConePage 会依据残留的 CurrentRoomState 导航进已死的房间
                    GlobalModel.CurrentRoomState = null;
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                    DialogHost.Show(new()
                    {
                        Title = "房间已断开连接",
                        Content = "当前房间已断开连接，可能房主已关闭房间，也可能您的网络环境有问题。",
                        CloseButtonText = "确定"
                    });
                }
            });
        };

        GlobalModel.GravityConeClient.OnResponse += (sender, eventArgs) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                if (eventArgs.Error == null)
                    return;

                var message = (eventArgs.Error != null && eventArgs.Error?.Message != null)
                    ? eventArgs.Error?.Message
                    : "未知错误";

                if (eventArgs.Error.Code == "ROOM_ALREADY_RUNNING")
                {
                    try
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

                        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                        {
                            if (GlobalModel.CurrentRoomState == null) return;
                            MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeLoadRoom(
                                GlobalModel.CurrentRoomState!.RoomType,
                                string.IsNullOrEmpty(GlobalModel.CurrentRoomState.RoomCode)
                                    ? null
                                    : GlobalModel.CurrentRoomState.RoomCode));
                        });
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                if (eventArgs.Error.Code == "INTERNAL_ERROR")
                {
                    message = "未检测到本地 Minecraft 基岩版房间，请尝试重启游戏，并在 Minecraft 中开启局域网游戏。";
                }

                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                });
                DialogHost.Show(new()
                {
                    Title = "出现错误",
                    Content = message,
                    CloseButtonText = "确定"
                });   
            });
        };
        
        GlobalModel.GravityConeClient.OnError += (sender, eventArgs) =>
        {
            // OnError 会因 CLI 的任意 stderr 输出行 / stdout 非 JSON 行而触发，
            // 大多是日志噪声。此前的实现是弹模态框并把用户踢回大厅，
            // 会导致正常联机过程中被随机中断。
            // 真正的致命状态（连接断开、端口占用等）由上面的 OnEvent 分支处理，
            // 这里只做非阻塞通知与日志记录。
            Console.WriteLine($@"[GravityCone] {eventArgs.Message}");
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                GlobalModel.MainWindow?.Notice?.AddNotice(new()
                {
                    Title = "联机组件警告",
                    Message = eventArgs.Message,
                    NoticeType = NoticeType.Warning
                });
            });
        };

        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            LoadInitBar.IsIndeterminate = false;
            LoadInitBar.Value = 100;

            MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
        });
    }
}