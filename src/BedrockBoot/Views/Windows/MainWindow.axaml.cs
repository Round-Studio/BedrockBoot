using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Pages;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.VersionJsons;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : OnePointWindow
{
    public MainWindow()
    {
        GlobalModel.MainWindow = this;
        InitializeComponent();

        UpdateBack();

        if (!Directory.Exists(PathsList.TempPath)) Directory.CreateDirectory(PathsList.TempPath);
        
        if (GlobalModel.Config.Data.WindowInfo.X != -1 && GlobalModel.Config.Data.WindowInfo.Y != -1)
        {
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Position = new PixelPoint(x: GlobalModel.Config.Data.WindowInfo.X,
                y: GlobalModel.Config.Data.WindowInfo.Y);

            this.Width = GlobalModel.Config.Data.WindowInfo.Width;
            this.Height = GlobalModel.Config.Data.WindowInfo.Height;

            Console.WriteLine(
                $@"Main Window: Width {GlobalModel.Config.Data.WindowInfo.Width}, Height {GlobalModel.Config.Data.WindowInfo.Height}");
        }

#if DEBUG
        DebugModule.IsVisible = true;
        VersionBox.IsVisible = false;
#endif

        MainFrame.NavigateTo(new LoadingPage());
        VersionBox.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        BuildTime.Text =
            $"Build.2.{((DateTime)(CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly()))).ToString("yy.MMdd.HHmmss")}";
        Task.Run(async () =>
        {
            GlobalModel.FunctionOption = new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>("avares://BedrockBoot/Manifest/Function/FunctionOption.json")
                .Result;

            try
            {
                GlobalModel.BedrockCore = new BedrockCore()
                {
                    Options = new CoreOptions()
                    {
                        IsAutoCompleteVC = true,
                        IsAutoOpenDevelopment = true,
                        IsCheckMD5 = true
                    }
                };
                Console.WriteLine("初始化核心完毕");
            }
            catch
            {
                Console.WriteLine("不支持该系统");
                DialogHost.Show(new DialogInfo()
                {
                    Title = "当前系统不支持",
                    Content = "根据我们的最低支持标准，系统版本号需要大于等于 19041\n" +
                              "请尝试升级系统后再次尝试",
                    CloseButtonText = "退出",
                    CloseAction = (() => Environment.Exit(1))
                });
            }

            try
            {
/*#if RELEASE
                if (GlobalModel.FunctionOption.IsEnableWebProtocol)
                    OpenProtocol();
#else
                OpenProtocol();
#endif*/

                var lst = await VersionsHelper.GetBuildDatabaseAsync(
                    "https://data.mcappx.com/v2/bedrock.json");
                Console.WriteLine("版本列表获取完毕");
            }
            catch (InvalidOperationException invEx)
            {
                Console.WriteLine("无法连接至清单服务器");
                DialogHost.Show(new DialogInfo()
                {
                    Title = "Emm...",
                    Content = "偶，您好像没有连接网络.jpg\n" +
                              "请尝试重新连接网络或切换网络环境后重试。",
                    CloseButtonText = "确定"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生初始化错误：{ex}");

                if (!GlobalModel.BedrockCore.GetWindowsDevelopmentState())
                {
                    Console.WriteLine("无法自动打开开发者模式");
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "开发者模式",
                        Content = "我们貌似无法帮您打开开发者模式，请您手动前往设置中打开。",
                        CloseButtonText = "确定"
                    });
                }
            }
            finally
            {
                Console.WriteLine("跳转主页面.jpg");
                Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new MainPage()));

#if DEBUG
                var date = CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly());
                var zt = CheckVersion.CheckTimeAndExecute24Hour((DateTime)date);
                Console.WriteLine(@$"当前模式：Debug 模式");
                Console.WriteLine(@$"当前程序集编译日期：{date}");
                Console.WriteLine(@$"当前版本是否在可用时间段内：{zt}");

                if (zt)
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content =
                            $"当前版本为预览版本，请勿添加到整合包中使用。\n当前版本仅作为测试部分功能，将于 24h 后失效，请抓紧时间进行测试。\n当前可用状态：{zt}",
                        Title = "版本模式提示",
                        CloseButtonText = "开始测试",
                        AccountButton = DialogButtons.CloseButton
                    });
                }
                else
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content =
                            $"当前版本为预览版本，请勿添加到整合包中使用。\n当前版本仅作为测试部分功能，将于 24h 后失效，当前已失效。\n当前可用状态：{zt}",
                        Title = "版本模式提示",
                        CloseButtonText = "退出",
                        CloseAction = () => { Environment.Exit(0); },
                        AccountButton = DialogButtons.CloseButton
                    });
                }
#endif

                if (!GlobalModel.Config.Data.IsAgreeTerms)
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content =
                            $"欢迎使用 BedrockBoot，\n开始使用即代表您同意此条款：\n\n" +
                            $"1. 此为非官方 Minecraft 启动器\n" +
                            $"2. 您需拥有合法授权的 Minecraft 副本，否则自动进入试玩版\n" +
                            $"3. 我们不会 辅助 / 协助 任何破解正版 Minecraft 的行为" +
                            $"4. 禁止任何形式的盗版或作弊行为\n" +
                            $"5. 模组 / 资源包 使用风险自负\n" +
                            $"6. 与 Mojang / Microsoft 无关联\n" +
                            $"7. 本软件为开源软件，使用和分发其副本源码请遵循开源协议 (GPL-v3)\n\n" +
                            $"继续使用即为接受条款",
                        Title = "BedrockBoot 用户使用协议",
                        CloseButtonText = "我同意",
                        CloseAction = () =>
                        {
                            GlobalModel.Config.Data.IsAgreeTerms = true;
                            GlobalModel.Config.Save();
                        },
                        PrimaryButtonText = "我不同意，退出",
                        PrimaryAction = () => { Environment.Exit(0); },
                        AccountButton = DialogButtons.CloseButton
                    });
                }
            }
        });
    }

    public void OpenProtocol()
    {
        var pro = new ProtocolRegister();
        pro.ProtocolName = "BedrockBoot";
        pro.ProtocolDescription = "BedrockBoot 协议";
        pro.RegisterProtocol(Process.GetCurrentProcess().MainModule.FileName);

        GlobalModel.ProtocolService.StartAsync();
        GlobalModel.ProtocolService.Get("/shell",async (context, parameters) =>
        {
            parameters.TryGetQuery("command", out var command);
            Console.WriteLine(command);

            var comm = command.Replace("bedrockboot://", "").Split('/');
            ProtocolCommand.OnCommand(comm);
                    
            await ProtocolService.WriteResponseAsync(context, 200, "ok");
        });
                
        Console.WriteLine("协议服务器启动成功！");
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        GlobalModel.Config.Data.WindowInfo = new ()
        {
            Width = this.Bounds.Width,
            Height = this.Bounds.Height,
            X = this.Position.X,
            Y = this.Position.Y
        };
        
        GlobalModel.Config.Save();
    }

    public async Task UpdateBack()
    {
        #region 更新背景

        this.TransparencyLevelHint = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Mica)
        {
            this.TransparencyLevelHint = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.Mica };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Blur)
        {
            this.TransparencyLevelHint = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.AcrylicBlur };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image)
        {
            BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;

            var index = GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex;
            if (index != -1)
            {
                if (GlobalModel.Config.Data.StyleConfig.BackgroundImages.Count >= 0)
                {
                    BackgroundBox.IsVisible = true;
                    SetBackgroundBlur(GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur);

                    BackgroundImage.Background = new ImageBrush()
                    {
                        Stretch = Stretch.UniformToFill,
                        Source = new Bitmap(
                            GlobalModel.Config.Data.StyleConfig.BackgroundImages[
                                GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex])
                    };
                }
            }

        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.AccentColor)
        {
            AccentBackgroundBox.IsVisible = true;
            AccentBackgroundBox.Opacity = 0.7;
        }

        #endregion
    }

    public void SetBackgroundBlur(int num)
    {
        if(num != 0)
        {
            BackgroundBox.Effect = new BlurEffect()
            {
                Radius = num
            };
            BackgroundBox.Margin = new Thickness(-num);
        }
        else
        {
            BackgroundBox.Effect = null;
            BackgroundBox.Margin = new Thickness(0);
        }
        BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }
}