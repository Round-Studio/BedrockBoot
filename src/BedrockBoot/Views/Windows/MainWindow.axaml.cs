using System;
using System.Collections.Generic;
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
using BedrockBoot.Base.Helper;
using BedrockBoot.Entity;
using BedrockBoot.Models;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Media;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Models.Style;
using BedrockBoot.Service;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Service.Protocol.Routes;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.SetupPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Style.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;
using Wallpaper.Avalonia.Controls;

#if WINDOWS
using BedrockBoot.Models.Helper.Uwp;
#endif

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : BedrockBootWindow
{
    public MainWindow()
    {
        // 核心引擎异步初始化
        _ = InitBedrockCoreAsync();

        InitializeComponent();
        GlobalModel.MainWindow = this;
        
        Core.Global.GlobalModel.Config.AddAfterSaveCallback(entity =>
        {
            IsolationPolicyHelper.PublicCatalogStrategy = Core.Global.GlobalModel.Config.Data.CatalogStrategy;
        });
        
        if (!Core.Global.GlobalModel.Config.Data.IsFirstRun) MainFrame.NavigateTo(new MainPage());
        else MainFrame.NavigateTo(new SetupRoot());
        InitializeWindowBounds();
        UpdateTheme();

        // 绑定回调
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        EasyLauncher.LaunchedBehavior = () => Dispatcher.UIThread.Invoke(RunBehavior);

        SetupDynamicHotkey();
        StartNetworkMonitoring();
        _ = InitializeAsync();

        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        _ = GetDevelopMode();
        CheckUwpDependence();

#if WINDOWS
        BedrockbootProtocolRegistration.Register();
#endif
        InitializeProtocolRoutes();
    }

    private I18nManager I18n => I18nManager.Instance;
    public bool IsWindowActive => IsActive;
    private DesktopThumbnailWindow? DesktopThumbnailWindow { get; set; }

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

        if (e.DataTransfer.Contains(DataFormat.File))
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
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        DropBox.Opacity = 0;
        SetBlurState(false);
        await Task.Delay(360);
        DropBox.IsVisible = false;

        var storageFiles = new List<IStorageFile>();
        foreach (var item in e.DataTransfer.Items)
        {
            if (item.TryGetFile() is IStorageFile file)
                storageFiles.Add(file);
        }
        if (storageFiles.Count <= 0) return;

        var paths = storageFiles.Select(f => f.Path.LocalPath).ToArray();
        Console.WriteLine($@"本次拖拽共 {paths.Length} 个文件。");
        foreach (var filePath in paths)
        {
            if (!string.IsNullOrEmpty(filePath))
                Console.WriteLine($@"检测到拖入文件: {filePath}");
        }

        OpenDraw(new DrawDropFileContent(storageFiles.ToArray()), "拖拽文件处理");
    }



    #endregion

    #region 初始化流程

    private async Task GetDevelopMode()
    {
#if WINDOWS
        var devMod = DeveloperModeHelper.IsDeveloperModeViaPowerShell();
        if (!devMod)
            DeveloperModeHelper.ShowNotice();
#endif
    }

    private void CheckUwpDependence()
    {
#if WINDOWS
        Task.Run(() =>
        {
            Thread.Sleep(1000);
            var depList = UwpDependencyChecker.GetMissingDependencies();
            if (depList.Count > 0)
            {
                Console.WriteLine($@"当前系统未安装对应的 UWP 依赖，共 {depList.Count} 个依赖未安装。");
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "安装 UWP 依赖",
                        Content = new DialogDownloadUwpDependenceContent(depList)
                    });
                });
            }
        });
#endif
    }

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
            Console.WriteLine($@"Failed to load FunctionOption: {ex.Message}");
        }

        CheckUserAgreement();

#if WINDOWS
        // 注册文件关联
        HandleFileAssociations();
#endif

        // 完成初始化后回到 UI 线程进行页面跳转
        await Dispatcher.UIThread.InvokeAsync(() => { LoadBox.IsVisible = false; });

        await BedrockbootProtocolHandler.ExecutePendingCommandAsync();
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

    private void InitializeProtocolRoutes()
    {
        ProtocolRouteRegistry.Instance.Register(new AboutProtocolRoute());
    }

    private async Task InitBedrockCoreAsync()
    {
        try
        {
            await CoreInit.Init();
            
            CoreInit.UpdateUseHardwareDecode(Core.Global.GlobalModel.Config.Data.IsUseHardwareDecode);
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

    private void UpdateBack()
    {
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
            case StyleType.LiveModel:
                if (DesktopThumbnailWindow == null)
                {
                    DesktopThumbnailWindow = new DesktopThumbnailWindow();
                }
                if (style.LiveBlur)
                {
                    TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };
                }
                DesktopThumbnailWindow?.ShowBelow(this);
                LiveOpacity.IsVisible = true;
                UpdateLiveOpacity();
                break;
        }
    }

    public void UpdateLiveOpacity()
    {
        LiveOpacity.Opacity =
            (100 - Core.Global.GlobalModel.Config.Data.StyleConfig.LiveOpacity) * 0.01;
    }

    private void ApplyImageBackground(StyleConfig style)
    {
        var imgPath = style.BackgroundImage;
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
            Console.WriteLine($@"Background image render error: {ex.Message}");
        }
    }

    public void SetBackgroundBlur(int radius)
    {
        if (radius > 0)
        {
            // 复用同一个 BlurEffect 实例，避免每次都 new 一份离屏渲染目标
            if (BackgroundBox.Effect is not BlurEffect blur)
            {
                blur = new BlurEffect();
                BackgroundBox.Effect = blur;
            }
            blur.Radius = radius;
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

    public void ReSetBackground()
    {
        // 状态重置
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        AnimationBackground.IsVisible = false;
        LiveOpacity.IsVisible = false;

        if (DesktopThumbnailWindow != null)
        {
            DesktopThumbnailWindow.Close();
            DesktopThumbnailWindow = null;
        }
    }

    public void UpdateTheme()
    {
        var musicName = Core.Global.GlobalModel.Config.Data.StyleConfig.BackgroundMusic;
        if (Core.Global.GlobalModel.Config.Data.StyleConfig.IsUseThemePack)
        {
            Task.Run(() =>
            {
                var packConfig =
                    ThemePackManager.GetPackManifestWithHash(Core.Global.GlobalModel.Config.Data.StyleConfig
                        .SelectThemePackHash);

                if (Core.Global.GlobalModel.Config.Data.StyleConfig.MediaSource == MediaSourceEnum.PriorityThemePack)
                {
                    if (!string.IsNullOrEmpty(packConfig.BackgroundMusicFileName) &&
                        File.Exists(packConfig.BackgroundMusicFileName))
                        musicName = packConfig.BackgroundMusicFileName;
                }

                if (Core.Global.GlobalModel.Config.Data.StyleConfig.MediaSource == MediaSourceEnum.OnlyThemePack)
                    musicName = packConfig.BackgroundMusicFileName;

                MediaManager.Instance.Play(musicName);

                Dispatcher.UIThread.Invoke(() =>
                {
                    ReSetBackground();
                    ApplyImageBackground(new StyleConfig()
                    {
                        Background3D = packConfig.BackgroundUse3D,
                        BackgroundImage = packConfig.BackgroundImageFileName,
                        BackgroundImageOpacity = packConfig.BackgroundImageOpacity,
                        BackgroundImageBlur = packConfig.BackgroundImageBlur
                    });

                    App.LoadColor(packConfig.ThemeColor,
                        packConfig.ThemeType);
                });
            });
        }
        else
        {
            ReSetBackground();
            UpdateBack();
            App.LoadColor(AccentColor.Colors[Core.Global.GlobalModel.Config.Data.StyleConfig.AccentColorIndex],
                Core.Global.GlobalModel.Config.Data.StyleConfig.LightThemeType);

            MediaManager.Instance.Play(musicName);
        }
    }

    #endregion

    #region 任务与交互

    public void UpdateTaskUI()
    {
        TaskList.ItemsSource = null;
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

            var visible = new System.Collections.Generic.List<Avalonia.Controls.Control>(tasks.Count);
            foreach (var task in tasks)
            {
                if (task.Item == null) continue;
                task.Item.Margin = new Thickness(5);
                visible.Add(task.Item);
            }
            // 一次性绑定，ListBox + VirtualizingStackPanel 会按需实例化
            TaskList.ItemsSource = visible;
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