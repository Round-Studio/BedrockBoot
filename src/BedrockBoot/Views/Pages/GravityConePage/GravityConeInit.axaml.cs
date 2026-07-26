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

        await ThirdTaskAsync();
    }

    private async Task GetNodesAsync()
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
            }
            else
            {
                Console.WriteLine(@"反序列化结果为空。");
                throw new Exception();
            }
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
                    CloseAction = () => { MainPage.Instance.SelTag.SelectedIndex = 0; }
                });
            });
        }
    }

    private async Task GetXboxUserAsync()
    {
        var checker = new XboxLoginStatusChecker();
        var status = await checker.GetDetailedXboxStatus();

        if (!status.IsLoggedIn)
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
                    CloseAction = () => { MainPage.Instance.SelTag.SelectedIndex = 0; }
                });
            });
        }
        else if (status.XboxUserInfo == null || string.IsNullOrEmpty(status.XboxUserInfo.Gamertag))
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                this.LoadGetPeersBar.IsIndeterminate = false;
                this.LoadGetPeersBar.Value = 0;
                DialogHost.Show(new()
                {
                    Title = "获取 Xbox 用户失败",
                    Content = "无法获取 Xbox 用户，请尝试重新登录 Xbox 账户",
                    CloseAction = () => { MainPage.Instance.SelTag.SelectedIndex = 0; }
                });
            });
        }
        else
        {
            GlobalModel.XboxUserInfo = status.XboxUserInfo;
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                this.LoadGetXboxUserBar.IsIndeterminate = false;
                this.LoadGetXboxUserBar.Value = 100;
            });
        }
    }

    private async Task ThirdTaskAsync()
    {
        await Task.Delay(100);
        GlobalModel.GravityConeClient = new GravityConeClient();
        await GlobalModel.GravityConeClient.StartAsync(
            Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath,
                "gravitycone-cli-windows-amd64.exe"),
            GlobalModel.ETPublicServer, $"\"BedrockBoot {GlobalModel.BodyVersion}\"", "\"BedrockBoot 联机房间\"",
            DialogDownloadMultiPlayerDependenceContent.GravityConeExePath);

        GlobalModel.GravityConeClient.OnEvent += (sender, eventArgs) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                if (eventArgs.Event == "paperconnect.connection.error")
                {
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
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());

                DialogHost.Show(new()
                {
                    Title = "出现错误",
                    Content = eventArgs.Message,
                    CloseButtonText = "确定"
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