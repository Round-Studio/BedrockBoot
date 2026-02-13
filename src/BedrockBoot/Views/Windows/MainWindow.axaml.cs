using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Service;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.SetupPage;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using CommunityToolkit.Mvvm.Input;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;
using Round.SDK.Helper;
using TextBlock = Avalonia.Controls.TextBlock;
using TextBox = Avalonia.Controls.TextBox;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : BedrockBootWindow
{
    public MainWindow()
    {
        GlobalModel.MainWindow = this;
        InitializeComponent();
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        SetupDynamicHotkey();

        UpdateBack();

        if (!Directory.Exists(PathsList.TempPath)) Directory.CreateDirectory(PathsList.TempPath);

        if (GlobalModel.Config.Data.WindowInfo.X != -1 && GlobalModel.Config.Data.WindowInfo.Y != -1)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(GlobalModel.Config.Data.WindowInfo.X,
                GlobalModel.Config.Data.WindowInfo.Y);

            Width = GlobalModel.Config.Data.WindowInfo.Width;
            Height = GlobalModel.Config.Data.WindowInfo.Height;

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
            $"Build.2.{((DateTime)CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly())).ToString("yy.MMdd.HHmmss")}";
        Task.Run(async () =>
        {
            GlobalModel.FunctionOption = new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>(
                    "avares://BedrockBoot/Manifest/Function/FunctionOption.json")
                .Result;

#if RELEASE
            if (GlobalModel.FunctionOption.IsEnableMcPackOpenWithBody)
                OpenAgreement.RegisterAssociation();
#else
            OpenAgreement.RegisterAssociation();
#endif

            try
            {
                GlobalModel.BedrockCore = new BedrockCore
                {
                    Options = new CoreOptions
                    {
                        IsAutoCompleteVC = true,
                        IsAutoOpenDevelopment = true,
                        IsAutoCompleteGameInput = true,
                        IsCheckMD5 = true
                    }
                };
                await GlobalModel.BedrockCore.InitAsync();
                Console.WriteLine(@"初始化核心完毕");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (ex.Message.Contains("Not Support Windows Version"))
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "当前系统不支持",
                        Content = "根据我们的最低支持标准，系统版本号需要大于等于 19041\n" +
                                  "请尝试升级系统后再次尝试",
                        CloseButtonText = "退出",
                        CloseAction = () => Environment.Exit(1)
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
                Console.WriteLine(@"版本列表获取完毕");
            }
            catch (InvalidOperationException invEx)
            {
                Console.WriteLine(@"无法连接至清单服务器");
                DialogHost.Show(new DialogInfo
                {
                    Title = "Emm...",
                    Content = "偶，您好像没有连接网络.jpg\n" +
                              "请尝试重新连接网络或切换网络环境后重试。",
                    CloseButtonText = "确定"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"发生初始化错误：{ex}");

                if (!GlobalModel.BedrockCore.GetWindowsDevelopmentState())
                {
                    Console.WriteLine(@"无法自动打开开发者模式");
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "开发者模式",
                        Content = "我们貌似无法帮您打开开发者模式，请您手动前往设置中打开。",
                        CloseButtonText = "确定"
                    });
                }
            }
            finally
            {
                if (GlobalModel.Config.Data.IsFirstRun)
                {
                    Console.WriteLine(@"跳转初始化向导页面.jpg");
                    Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new SetupRoot()));
                }
                else
                {
                    Console.WriteLine(@"跳转主页面.jpg");
                    Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new MainPage()));
                }
                if (!GlobalModel.Config.Data.IsAgreeTerms)
                    DialogHost.Show(new DialogInfo
                    {
                        Content =
                            $"欢迎使用 BedrockBoot，\n开始使用即代表您同意此条款：\n\n" +
                            $"1. 此为非官方 Minecraft 启动器\n" +
                            $"2. 您需拥有合法授权的 Minecraft 副本，否则自动进入试玩版\n" +
                            $"3. 我们不会 辅助 / 协助 任何破解正版 Minecraft 的行为\n" +
                            $"4. 禁止任何形式的盗版或作弊行为\n" +
                            $"5. 模组 / 资源包 使用风险自负\n" +
                            $"6. 与 Mojang / Microsoft 无关联\n" +
                            $"7. 本软件会修改注册表相关配置\n" +
                            $"8. 本软件为开源软件，使用和分发其副本源码请遵循开源协议 (GPL-v3)\n\n" +
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
        });
    }

    public void OpenProtocol()
    {
        var pro = new ProtocolRegister();
        pro.ProtocolName = "BedrockBoot";
        pro.ProtocolDescription = "BedrockBoot 协议";
        pro.RegisterProtocol(Process.GetCurrentProcess().MainModule.FileName);

        GlobalModel.ProtocolService.StartAsync();
        GlobalModel.ProtocolService.Get("/shell", async (context, parameters) =>
        {
            parameters.TryGetQuery("command", out var command);
            Console.WriteLine(command);

            var comm = command.Replace("bedrockboot://", "").Split('/');
            ProtocolCommand.OnCommand(comm);

            await ProtocolService.WriteResponseAsync(context, 200, "ok");
        });

        Console.WriteLine(@"协议服务器启动成功！");
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        GlobalModel.Config.Data.WindowInfo = new WindowInfo
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            X = Position.X,
            Y = Position.Y
        };

        GlobalModel.Config.Save();
    }

    public async Task UpdateBack()
    {
        #region 更新背景

        TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        AnimationBackground.IsVisible = false;
        if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Mica)
        {
            TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.Mica };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Blur)
        {
            TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.AcrylicBlur };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image)
        {
            BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;

            var index = GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex;
            if (index != -1)
                if (GlobalModel.Config.Data.StyleConfig.BackgroundImages.Count >= 0)
                {
                    BackgroundBox.IsVisible = true;
                    SetBackgroundBlur(GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur);

                    BackgroundImage.Background = new ImageBrush
                    {
                        Stretch = Stretch.UniformToFill,
                        Source = new Bitmap(
                            GlobalModel.Config.Data.StyleConfig.BackgroundImages[
                                GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex])
                    };
                }
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.AccentColor)
        {
            AccentBackgroundBox.IsVisible = true;
            AccentBackgroundBox.Opacity = 0.7;
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Voronoi)
        {
            AnimationBackground.IsVisible = true;
            AnimationBackground.BackgroundType = BackgroundType.Voronoi;
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Bubble)
        {
            AnimationBackground.IsVisible = true;
            AnimationBackground.BackgroundType = BackgroundType.Bubble;
        }

        #endregion
    }

    public void SetBackgroundBlur(int num)
    {
        if (num != 0)
        {
            BackgroundBox.Effect = new BlurEffect
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
    
    private void SetupDynamicHotkey()
    {
        this.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private async Task HandlePasteAsync()
    {
        Console.WriteLine("Ctrl+V 被按下");
        CopyService.HandleCopyAction();
    }
    
    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            // 检查事件源是否是输入控件
            var source = e.Source;
            if (!(source is TextBox || source is TextBlock || source is RichTextBox))
            {
                await HandlePasteAsync();
                e.Handled = true;
            }
        }
    }

    private void TaskBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsTaskCardOpen) CloseTaskCard();
        else OpenTaskCard();
    }

    public void UpdateTaskUI()
    {
        TaskList.Children.Clear();
        TaskViewer.IsVisible = true;
        NoneBox.IsVisible = false;
        TaskInfoText.IsVisible = true;
        TaskInfoText.Text = $"当前有 {GlobalModel.TaskManager.Tasks.Count} 项任务";

        if (GlobalModel.TaskManager.Tasks.Count <= 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
            TaskInfoText.IsVisible = false;
        }

        GlobalModel.TaskManager.Tasks.ForEach(task =>
        {
            task.Item.Margin = new Thickness(5);
            TaskList.Children.Add(task.Item);
        });
    }
}