/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Core.Models.Xbox;
using BedrockBoot.GravityCone;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

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
    /// 返回主页。MainPage.Instance 在使用 MainPage（Beta UI）时为 null，
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
                    Title = I18nManager.Instance["GravityCone.Error.GetPeersFailed"],
                    Content = I18nManager.Instance["GravityCone.Error.GetPeersFailed.Desc"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
                    CloseAction = BackToMain
                });
            });
            return false;
        }
    }

    private async Task<bool> GetXboxUserAsync()
    {
#if LINUX
        if (MsAccountManager.Accounts.Accounts.Count <= 0)
            return false;
        GlobalModel.XboxUserInfo = new()
        {
            Gamertag = MsAccountManager.Accounts.Accounts.Find(x => x.BUID == MsAccountManager.Accounts.SelectUserBUID)
                .UserName
        };
        
        return true;
#endif
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
                    Title = I18nManager.Instance["GravityCone.Error.GetXboxUserFailed"],
                    Content = I18nManager.Instance["GravityCone.Error.GetXboxUserFailed.Desc"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
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
            var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            // Client 已改用 ArgumentList 传参，vendor/motd 不需要再嵌入引号
            await client.StartAsync(
                Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath,
                    $"gravitycone-cli-{OS.GetSystemType()}-amd64{ext}"),
                GlobalModel.ETPublicServer, $"BedrockBoot {GlobalModel.BodyVersion}", I18nManager.Instance["GravityCone.Room.Motd"],
                DialogDownloadMultiPlayerDependenceContent.GravityConeExePath);
        }
        catch (Exception ex)
        {
            // 启动失败时必须释放并保持 GlobalModel.GravityConeClient 为 null：
            // 否则 MainGravityConePage 会把残留的半初始化客户端当作可用状态，
            // 之后所有请求都会失败且用户永远停在加载页。
            Console.WriteLine($@"GravityCone CLI 启动失败: {ex}");
            try
            {
                client.Dispose();
            }
            catch
            {
            }

            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                LoadInitBar.IsIndeterminate = false;
                LoadInitBar.Value = 0;
                DialogHost.Show(new()
                {
                    Title = I18nManager.Instance["GravityCone.Error.StartFailed"],
                    Content = string.Format(I18nManager.Instance["GravityCone.Error.StartFailed.Desc"], ex.Message),
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
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
                        Title = I18nManager.Instance["Shared.Status.Error.Title"],
                        Content = I18nManager.Instance["GravityCone.Error.NoGameComm"],
                        CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
                    });
                }

                if (eventArgs.Event == "paperconnect.connection.port_busy")
                {
                    GlobalModel.CurrentRoomState = null;
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                    DialogHost.Show(new()
                    {
                        Title = I18nManager.Instance["Shared.Status.Error.Title"],
                        Content = I18nManager.Instance["GravityCone.Error.PortInUse"],
                        CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
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
                        Title = I18nManager.Instance["GravityCone.Error.RoomDisconnected"],
                        Content = I18nManager.Instance["GravityCone.Error.RoomDisconnected.Desc"],
                        CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
                    });
                }
            });
        };

        GlobalModel.GravityConeClient.OnResponse += (sender, eventArgs) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (eventArgs.Error == null)
                    return;

                var message = (eventArgs.Error != null && eventArgs.Error?.Message != null)
                    ? eventArgs.Error?.Message
                    : I18nManager.Instance["GravityCone.Error.Unknown"];

                if (eventArgs.Error.Code == "ROOM_ALREADY_RUNNING")
                {
                    try
                    {
                        try
                        {
                            // 先 await 停止/离开房间，异步异常才能被捕获；完成后再导航重试
                            var gravityConeClient = GlobalModel.GravityConeClient;
                            if (gravityConeClient != null)
                            {
                                if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Host)
                                    await gravityConeClient.StopRoomAsync();
                                if (GlobalModel.CurrentRoomState?.RoomType == RoomType.Guest)
                                    await gravityConeClient.LeaveRoomAsync();
                            }
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
                    message = I18nManager.Instance["GravityCone.Error.NoLocalGame"];
                }

                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeRoot());
                });
                DialogHost.Show(new()
                {
                    Title = I18nManager.Instance["Shared.Status.Error.Title"],
                    Content = message,
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
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
                    Title = I18nManager.Instance["GravityCone.Notice.Warning"],
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