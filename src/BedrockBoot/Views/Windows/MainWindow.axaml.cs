using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum;
using BedrockBoot.Entity;
using BedrockBoot.Models;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Service;
using BedrockBoot.Service.WebServer;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.SetupPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : BedrockBootWindow
{
    public MainWindow()
    {
        // 核心引擎异步初始化
        _ = InitBedrockCoreAsync();

        InitializeComponent();
        GlobalModel.MainWindow = this;
        
        if (!Core.Global.GlobalModel.Config.Data.IsFirstRun) MainFrame.NavigateTo(new MainPage());
        else MainFrame.NavigateTo(new SetupRoot());
        UpdateBack();
        InitializeWindowBounds();

        // 绑定回调
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        EasyLauncher.LaunchedBehavior = () => Dispatcher.UIThread.Invoke(RunBehavior);

        SetupDynamicHotkey();
        StartNetworkMonitoring();
        _ = InitializeAsync();

        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        
        InitializeWindowBounds();
    }

    private I18nManager I18n => I18nManager.Instance;
    public bool IsWindowActive => IsActive;

    #region 窗口拖拽事件

    /// <summary>
    ///     当文件拖拽到窗口上方时触发，决定是否显示“拷贝”图标
    /// </summary>
    private async void OnDragOver(object? sender, DragEventArgs e)
    {
        var position = e.GetPosition(this);

        // 哪怕系统认为还在 Over，但只要坐标出了窗口，立即转为隐藏流程
        if (position.X < 10 || position.Y < 10 || 
            position.X > Bounds.Width - 10 || position.Y > Bounds.Height - 10)
        {
            e.DragEffects = DragDropEffects.None;
            HideDropBox();
            return;
        }

        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
            DropBox.IsVisible = true;
            DropBox.Opacity = 1;
            SetBlurState(true);
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            HideDropBox();
        }
    }
    
    private async void HideDropBox()
    {
        if (!DropBox.IsVisible) return; // 避免重复触发
    
        DropBox.Opacity = 0;
        SetBlurState(false);
        await Task.Delay(360);
        DropBox.IsVisible = false;
    }

    /// <summary>
    ///     当用户松开鼠标完成放置时触发
    /// </summary>
    [Obsolete("Obsolete")]
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        // 获取文件路径列表
        var files = e.Data.GetFiles().OfType<IStorageFile>().ToArray();;
        
        DropBox.Opacity = 0;
        SetBlurState(false);
        await Task.Delay(360);
        DropBox.IsVisible = false;

        if (files != null)
        {
            var storageItems = files as IStorageItem[] ?? files.ToArray();
            Console.WriteLine($@"本次拖拽共 {storageItems.Length} 个文件。");
            if (storageItems.Length <= 0)
                return;
            
            foreach (var file in storageItems)
            {
                // 获取文件的绝对路径
                var filePath = file.Path.LocalPath;

                if (!string.IsNullOrEmpty(filePath)) Console.WriteLine($@"检测到拖入文件: {filePath}");
            }

            OpenDraw(new DrawDropFileContent(storageItems), "拖拽文件处理");
        }
    }

    #endregion

    #region 初始化流程

    private void InitializeWindowBounds()
    {
        if (Core.Global.GlobalModel.Config.Data.WindowInfo.X >= 1 &&
            Core.Global.GlobalModel.Config.Data.WindowInfo.Y >= 1)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(Core.Global.GlobalModel.Config.Data.WindowInfo.X,
                Core.Global.GlobalModel.Config.Data.WindowInfo.Y);
            Width = Core.Global.GlobalModel.Config.Data.WindowInfo.Width;
            Height = Core.Global.GlobalModel.Config.Data.WindowInfo.Height;
        }

#if DEBUG
        DebugModule.IsVisible = true;
        VersionBox.IsVisible = false;
#endif

        VersionBox.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        var buildTimestamp = (DateTime)CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly());
        BuildTime.Text = $"Build.2.{buildTimestamp:yy.MMdd.HHmmss}";

        if (!Directory.Exists(PathsList.TempPath))
            Directory.CreateDirectory(PathsList.TempPath);
    }

    private async Task InitializeAsync()
    {
        // 加载功能配置文件
        try
        {
            BedrockBoot.Models.Global.GlobalModel.FunctionOption = await new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>(
                    "avares://BedrockBoot/Manifest/Function/FunctionOption.json");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load FunctionOption: {ex.Message}");
        }

        CheckUserAgreement();

#if WINDOWS
        // 注册文件关联
        HandleFileAssociations();
#endif

        // 完成初始化后回到 UI 线程进行页面跳转
        await Dispatcher.UIThread.InvokeAsync(() => { LoadBox.IsVisible = false; });
    }

    private void HandleFileAssociations()
    {
#if RELEASE
        if (GlobalModel.FunctionOption?.IsEnableMcPackOpenWithBody == true)
            OpenAgreement.RegisterAssociation();
#else
        OpenAgreement.RegisterAssociation();
#endif
    }

    private async Task InitBedrockCoreAsync()
    {
        try
        {
            await CoreInit.Init();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"BedrockCore Init Error: {ex}");

            if (ex.Message.Contains("Not Support Windows Version"))
                await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(new DialogInfo
                {
                    Title = I18n["MainWindow.Dialog.UnsupportedSys.Title"],
                    Content = I18n["MainWindow.Dialog.UnsupportedSys.Content"],
                    CloseButtonText = I18n["MainWindow.Dialog.UnsupportedSys.Close"],
                    CloseAction = () => Environment.Exit(1)
                }));
        }
    }

    private void CheckUserAgreement()
    {
        if (Core.Global.GlobalModel.Config.Data.IsAgreeTerms) return;

        DialogHost.Show(new DialogInfo
        {
            Content = new DialogAgreementContent(),
            Title = I18n["MainWindow.Dialog.Agreement.Title"],
            CloseButtonText = I18n["MainWindow.Dialog.Agreement.Agree"],
            CloseAction = () =>
            {
                Core.Global.GlobalModel.Config.Data.IsAgreeTerms = true;
                Core.Global.GlobalModel.Config.Save();
            },
            PrimaryButtonText = I18n["MainWindow.Dialog.Agreement.Decline"],
            PrimaryAction = () => Environment.Exit(0),
            AccountButton = DialogButtons.CloseButton
        });
    }

    #endregion

    #region 视觉渲染 (背景处理)

    public void UpdateBack()
    {
        // 状态重置
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        AnimationBackground.IsVisible = false;

        var style = Core.Global.GlobalModel.Config.Data.StyleConfig;

        switch (style.StyleType)
        {
            case StyleType.Mica:
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica };
                break;
            case StyleType.Blur:
                TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };
                break;
            case StyleType.Image:
                ApplyImageBackground(style);
                break;
            case StyleType.AccentColor:
                AccentBackgroundBox.IsVisible = true;
                AccentBackgroundBox.Opacity = 0.7;
                break;
            case StyleType.Voronoi:
                AnimationBackground.IsVisible = true;
                AnimationBackground.BackgroundType = BackgroundType.Voronoi;
                break;
            case StyleType.Bubble:
                AnimationBackground.IsVisible = true;
                AnimationBackground.BackgroundType = BackgroundType.Bubble;
                break;
        }
    }

    private void ApplyImageBackground(StyleConfig style)
    {
        if (style.BackgroundImageSelectedIndex == -1 || style.BackgroundImages.Count == 0) return;

        var imgPath = style.BackgroundImages[style.BackgroundImageSelectedIndex];
        if (!File.Exists(imgPath)) return;

        try
        {
            BackgroundBox.IsVisible = true;
            BackgroundImage.IsVisible = false;
            BackgroundImage3D.IsVisible = false;

            SetBackgroundBlur(style.BackgroundImageBlur);

            var bitmap = new Bitmap(imgPath);
            if (style.Background3D)
            {
                BackgroundImage3D.IsVisible = true;
                BackgroundImage3D.Source = bitmap;
                BackgroundImage3D.Stretch = Stretch.UniformToFill;
            }
            else
            {
                BackgroundImage.IsVisible = true;
                BackgroundImage.Background = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill,
                    Source = bitmap
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Background image render error: {ex.Message}");
        }
    }

    public void SetBackgroundBlur(int radius)
    {
        if (radius > 0)
        {
            BackgroundBox.Effect = new BlurEffect { Radius = radius };
            BackgroundBox.Margin = new Thickness(-radius);
        }
        else
        {
            BackgroundBox.Effect = null;
            BackgroundBox.Margin = new Thickness(0);
        }

        // 透明度应用
        BackgroundImageOpacity.Opacity =
            (100 - Core.Global.GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }

    #endregion

    #region 任务与交互

    public void UpdateTaskUI()
    {
        TaskList.Children.Clear();
        var tasks = GlobalModel.TaskManager.Tasks;

        if (tasks.Count == 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
            TaskInfoText.IsVisible = false;
        }
        else
        {
            TaskViewer.IsVisible = true;
            NoneBox.IsVisible = false;
            TaskInfoText.IsVisible = true;
            TaskInfoText.Text = string.Format(I18n["MainWindow.Task.CountInfo"], tasks.Count);

            foreach (var task in tasks)
            {
                if (task.Item == null) continue;
                task.Item.Margin = new Thickness(5);
                TaskList.Children.Add(task.Item);
            }
        }
    }

    public void SetReboot()
    {
        this.RebootBtn.IsVisible = true;
    }

    private void RebootBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "重启启动器",
            Content = "当前需要重启。\n" +
                      "请问是否需要重启启动器？",
            CloseButtonText = "立即重启",
            PrimaryButtonText = "稍后重启",
            CloseAction = () =>
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName 
                              ?? throw new InvalidOperationException("无法获取可执行文件路径");
    
                var workingDir = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;
                var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1)
                    .Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
    
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
    
                // Windows 特殊处理
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    startInfo.Verb = "open";
                }
                
                Process.Start(startInfo);
                Environment.Exit(0);
            }
        });
    }

    private void RunBehavior()
    {
        switch (Core.Global.GlobalModel.Config.Data.LaunchBehavior)
        {
            case LaunchBehaviorEnum.Minimize:
                WindowState = WindowState.Minimized;
                break;
            case LaunchBehaviorEnum.Exit:
                Environment.Exit(0);
                break;
        }
    }

    private void SetupDynamicHotkey()
    {
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl + V 全局粘贴逻辑
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
            if (e.Source is not (TextBox or TextBlock))
            {
                CopyService.HandleCopyAction();
                e.Handled = true;
            }
    }

    private void TaskBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsTaskCardOpen) CloseTaskCard();
        else OpenTaskCard();
    }

    private CancellationTokenSource? _netMonitorCts;

    private void StartNetworkMonitoring()
    {
        _netMonitorCts = new CancellationTokenSource();
        var token = _netMonitorCts.Token;

        NetworkChange.NetworkAvailabilityChanged += (s, e) => { UpdateNetworkStatus(e.IsAvailable); };

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var isAlive = await CheckInternetConnectivityAsync();
                UpdateNetworkStatus(isAlive);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }, token);
    }

    private async Task<bool> CheckInternetConnectivityAsync()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("223.5.5.5", 2000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateNetworkStatus(bool isAvailable)
    {
        GlobalModel.IsNetworkAvailable = isAvailable;
        Dispatcher.UIThread.Invoke(() => { OfflineBtn.IsVisible = !GlobalModel.IsNetworkAvailable; });
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // 保存窗口状态
        Core.Global.GlobalModel.Config.Data.WindowInfo = new WindowInfo
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            X = Position.X,
            Y = Position.Y
        };
        Core.Global.GlobalModel.Config.Save();
    }

    #endregion
}