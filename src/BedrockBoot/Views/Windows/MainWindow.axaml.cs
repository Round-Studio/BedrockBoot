using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages;
using BedrockLauncher.Core;
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
        GlobalModel.FunctionOption = new JsonResourceEntity()
            .LoadJsonResourceAsync<FunctionOptionEntry>("avares://BedrockBoot/Manifest/Function/FunctionOption.json")
            .Result;
        GlobalModel.MainWindow = this;
        InitializeComponent();
        
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
#endif

        MainFrame.NavigateTo(new LoadingPage());
        VersionBox.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        BuildTime.Text =
            $"Build.2.{((DateTime)(CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly()))).ToString("yy.MMdd.HHmmss")}";
        Task.Run(async () =>
        {
            GlobalModel.BedrockCore = new BedrockCore();
            
            try
            {
                Console.WriteLine("初始化核心完毕");

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
                            $"1.此为非官方 Minecraft 启动器\n" +
                            $"2.您需拥有合法授权的 Minecraft 副本，否则自动进入试玩版\n" +
                            $"3.禁止任何形式的盗版或作弊行为\n" +
                            $"4.模组/资源包使用风险自负\n" +
                            $"5.与 Mojang/Microsoft 无关联\n" +
                            $"6.本软件为开源软件，使用和分发其副本源码请遵循开源协议 (GPL-v3)\n\n" +
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
}