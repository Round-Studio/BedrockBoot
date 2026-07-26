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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Base.Helper;
using BedrockBoot.Entity;
using BedrockBoot.Models;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Media;
using BedrockBoot.Models.Pack.System.DropFile;
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
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Notice.Info;
using Round.SDK.Helper;
using Wallpaper.Avalonia.Controls;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : Window
{
    private static List<string> _installedFontNames;

    public static List<string> InstalledFontNames
    {
        get
        {
            if (_installedFontNames == null)
                _installedFontNames = FontManager.Current.SystemFonts
                    .Select(f => f.Name)
                    .ToList();
            return _installedFontNames;
        }
    }

    private bool _ctrlPressed = false;
    public int DrawMarginLR = 10;
    private DispatcherTimer _volumeControlTimer;
    private DispatcherTimer _configRefreshDebounce;
    private DispatcherTimer _configSaveDebounce;

    private WindowState _lastWindowState;
    private bool? _lastUseSystemWindow;
    private bool? _lastIsBlurStyle;
    private string? _lastTitle;

    public MainWindow()
    {
        InitializeComponent();
        DialogHost.SetHost(DialogHost);
        GlobalModel.MainWindow = this;

        if (!Core.Global.GlobalModel.Config.Data.IsFirstRun)
            MainFrame.NavigateTo(Core.Global.GlobalModel.Config.Data.IsUseBetaUI ? new NeoMainPage() : new MainPage());
        else MainFrame.NavigateTo(new SetupRoot());
        InitializeWindowBounds();

        // 绑定回调（保存委托引用，便于窗口关闭时精确解绑）
        _taskChangedHandler = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        _launchedBehaviorHandler = () => Dispatcher.UIThread.Invoke(RunBehavior);
        GlobalModel.TaskManager.OnChanged += _taskChangedHandler;
        EasyLauncher.LaunchedBehavior = _launchedBehaviorHandler;

        SetupDynamicHotkey();
        StartNetworkMonitoring();
        _ = InitializeAsync();

        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        InitializeTaskbarProgress();
        InitRefreshTaskItemTask();

        MediaManager.Instance.Volume = (float)Math.Clamp(Core.Global.GlobalModel.Config.Data.MediaVolume, 0.0, 1.0);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        Deactivated += OnWindowDeactivated;
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        Frame.NavigateTo("");
        _volumeControlTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.8)
        };
        _volumeControlTimer.Tick += VolumeControlTimer_Tick;

        // 改用 PropertyChanged 事件驱动刷新窗口装饰，避免原先 100ms 轮询
        PropertyChanged += OnSelfPropertyChanged;
        Core.Global.GlobalModel.Config.AfterSave += OnConfigAfterSave;
        Closed += (_, _) =>
        {
            Core.Global.GlobalModel.Config.AfterSave -= OnConfigAfterSave;
            PropertyChanged -= OnSelfPropertyChanged;

            // 这两个静态字段捕获了 this，不清理会让 MainWindow 在进程生命周期内无法回收
            if (ReferenceEquals(EasyLauncher.LaunchedBehavior, _launchedBehaviorHandler))
                EasyLauncher.LaunchedBehavior = null;
            if (_taskChangedHandler != null)
                GlobalModel.TaskManager.OnChanged -= _taskChangedHandler;

            // 退出前把防抖队列中尚未落盘的配置修改写入磁盘
            Core.Global.ConfigSaveScheduler.Flush();
        };

        RefreshWindowChrome();
        BottomBorder.Margin = new Thickness(DrawMarginLR, 0, DrawMarginLR, 0);

        Loaded += (_, _) =>
        {
            Title = GlobalModel.CustomManifest.Title.Replace("{{version}}", GlobalModel.BodyVersion);
            HelpBtn.IsVisible = GlobalModel.CustomManifest.IsShowHelpBtn;
            if (GlobalModel.CustomManifest.IsShowHelpBtn)
            {
                var flyout = new MenuFlyout();
                HelpBtn.Flyout = flyout;
                GlobalModel.CustomManifest.HelpLinks.ForEach(link =>
                {
                    var item = new MenuItem()
                    {
                        Header = link.Name,
                        Icon = new FontIcon()
                        {
                            Glyph = link.Icon,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    item.Click += (_, _) =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = link.Link,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    };
                    flyout.Items.Add(item);
                });
            }
        };
    }

    public FontFamily GetFontFamily(string mainFont, string fallbackFont)
    {
        FontFamily combinedFont = new("DINPro, Noto Sans SC");

        if (mainFont == "DINPro")
            mainFont = "resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro";

        if (fallbackFont == "DINPro")
            fallbackFont = "resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro";

        if (!string.IsNullOrEmpty(mainFont) && !string.IsNullOrEmpty(fallbackFont))
            combinedFont = new FontFamily($"{mainFont}, {fallbackFont}");
        else if (!string.IsNullOrEmpty(mainFont))
            combinedFont = new FontFamily(mainFont);
        else if (!string.IsNullOrEmpty(fallbackFont)) combinedFont = new FontFamily(fallbackFont);

        GlobalModel.MainWindow.FontFamily = combinedFont;

        return combinedFont;
    }

#if WINDOWS
    private IntPtr _windowHandle;

    private double _lastReportedProgress = -1;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(100);
    private const double MinProgressDelta = 1;

    private void InitializeTaskbarProgress()
    {
        Opened += (sender, args) =>
        {
            _windowHandle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

            GlobalModel.TaskManager.AddOverallProgressCallback(progress =>
            {
                if (DateTime.Now - _lastUpdateTime < _minInterval &&
                    Math.Abs(progress - _lastReportedProgress) < MinProgressDelta)
                    return;

                _lastReportedProgress = progress;
                _lastUpdateTime = DateTime.Now;

                var hasRunningTasks = GlobalModel.TaskManager.Tasks
                    .Any(t => t.TaskItem is { IsCompleted: false });

                Dispatcher.UIThread.Post(() =>
                {
                    BedrockBoot.Windows.Models.TaskbarProgress.SetProgress(
                        _windowHandle, (int)progress, hasRunningTasks);
                });
            });
        };
    }
#else
    private void InitializeTaskbarProgress()
    {
    }
#endif

    private I18nManager I18n => I18nManager.Instance;
    public bool IsWindowActive => IsActive;
    private DesktopThumbnailWindow? DesktopThumbnailWindow { get; set; }

    #region 窗口拖拽事件

    private async void OnDragOver(object? sender, DragEventArgs e)
    {
        var position = e.GetPosition(this);

        if (position.X < 10 || position.Y < 10 ||
            position.X > Bounds.Width - 10 || position.Y > Bounds.Height - 10)
        {
            e.DragEffects = DragDropEffects.None;
            HideDropBox();
            return;
        }

        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null && files.Any())
            {
                var isValid = false;
                SupportedFileType? fileType = null;
                var displayName = "";
                var allowMany = false;
                var fileCount = 0;

                foreach (var file in files)
                {
                    fileCount++;
                    var extension = Path.GetExtension(file.Name).ToLowerInvariant();

                    if (GlobalKeys.DropOverTypesOfSupport.TryGetValue(extension, out var supportInfo))
                    {
                        if (!fileType.HasValue)
                        {
                            fileType = supportInfo.Type;
                            displayName = supportInfo.Name;
                            allowMany = supportInfo.AllowMany;
                            isValid = true;
                        }

                        if (fileType.Value != supportInfo.Type)
                        {
                            isValid = false;
                            break;
                        }
                    }
                    else
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    if (fileCount > 1 && !allowMany)
                    {
                        e.DragEffects = DragDropEffects.None;
                        HideDropBox();
                    }
                    else
                    {
                        e.DragEffects = DragDropEffects.Copy;
                        DropBox.IsVisible = true;
                        DropBox.Opacity = 1;
                        SetBlurState(true);
                    }
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                    HideDropBox();
                }
            }
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
        Task.Run(async () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                DropBox.Opacity = 0;
                SetBlurState(false);
            });
            await Task.Delay(360);
            Dispatcher.UIThread.Invoke(() => { DropBox.IsVisible = false; });
        });

        var storageFiles = new List<IStorageFile>();
        foreach (var item in e.DataTransfer.Items)
            if (item.TryGetFile() is IStorageFile file)
                storageFiles.Add(file);

        if (storageFiles.Count <= 0) return;

        var paths = storageFiles.Select(f => f.Path.LocalPath).ToArray();
        Console.WriteLine($@"本次拖拽共 {paths.Length} 个文件。");
        foreach (var filePath in paths)
            if (!string.IsNullOrEmpty(filePath))
                Console.WriteLine($@"检测到拖入文件: {filePath}");

        // OpenDraw(new DrawDropFileContent(storageFiles.ToArray()), "拖拽文件处理");
        var handler = new DropFileHandler(paths.ToList());
        handler.Handle();
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
    }

    private async Task InitializeAsync()
    {
        Task.Run(() =>
        {
            if (!Directory.Exists(PathsList.TempPath))
                Directory.CreateDirectory(PathsList.TempPath);
        });

        CoreInitialize.Init();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateTheme();
            LoadBox.IsVisible = false;
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
                if (DesktopThumbnailWindow == null) DesktopThumbnailWindow = new DesktopThumbnailWindow();

                if (style.LiveBlur) TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };

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

    private async void ApplyImageBackground(StyleConfig style)
    {
        var imgPath = style.BackgroundImage;
        if (!File.Exists(imgPath)) return;

        try
        {
            BackgroundBox.IsVisible = true;
            BackgroundImage.IsVisible = false;
            BackgroundImage3D.IsVisible = false;

            SetBackgroundBlur(style.BackgroundImageBlur);

            var bitmap = await Task.Run(() => new Bitmap(imgPath));
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
        GetFontFamily(Core.Global.GlobalModel.Config.Data.StyleConfig.MainFont,
            Core.Global.GlobalModel.Config.Data.StyleConfig.FallbackFont);
        MediaManager.Instance.Enabled = Core.Global.GlobalModel.Config.Data.IsPlayBackgroundMusic;
        var musicName = Core.Global.GlobalModel.Config.Data.StyleConfig.BackgroundMusic;
        if (Core.Global.GlobalModel.Config.Data.StyleConfig.IsUseThemePack)
        {
            Task.Run(() =>
            {
                var packConfig =
                    ThemePackManager.GetPackManifestWithHash(Core.Global.GlobalModel.Config.Data.StyleConfig
                        .SelectThemePackHash);

                if (packConfig == null)
                    return;

                if (Core.Global.GlobalModel.Config.Data.StyleConfig.MediaSource == MediaSourceEnum.PriorityThemePack)
                    if (!string.IsNullOrEmpty(packConfig.BackgroundMusicFileName) &&
                        File.Exists(packConfig.BackgroundMusicFileName))
                        musicName = packConfig.BackgroundMusicFileName;

                if (Core.Global.GlobalModel.Config.Data.StyleConfig.MediaSource == MediaSourceEnum.OnlyThemePack)
                    musicName = packConfig.BackgroundMusicFileName;

                MediaManager.Instance.Play(musicName);

                Dispatcher.UIThread.Invoke(() =>
                {
                    ReSetBackground();
                    ApplyImageBackground(new StyleConfig
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

            var music = musicName;
            Task.Run(() => MediaManager.Instance.Play(music));
        }
    }

    #endregion

    #region 任务与交互

    private DispatcherTimer? _taskRefreshTimer;

    /// <summary>持有回调委托引用，便于窗口关闭时精确解绑静态字段</summary>
    private Action? _taskChangedHandler;

    private Action? _launchedBehaviorHandler;

    public void InitRefreshTaskItemTask()
    {
        // 原实现为 Task.Run + while(true) + Thread.Sleep(30000)：
        // 占用一个永不退出且不可取消的线程，并以阻塞方式回调 UI 线程。
        // 改为 DispatcherTimer，可随窗口关闭停止。
        _taskRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _taskRefreshTimer.Tick += (_, _) => UpdateTaskUI();
        _taskRefreshTimer.Start();

        Closed += (_, _) => _taskRefreshTimer?.Stop();

        UpdateTaskUI();
    }

    /// <summary>上次绑定到任务列表的控件集合，用于跳过无变化的重建</summary>
    private List<Avalonia.Controls.Control>? _lastTaskItems;

    public void UpdateTaskUI()
    {
        var tasks = GlobalModel.TaskManager.Tasks;

        if (tasks.Count == 0)
        {
            if (_lastTaskItems is { Count: > 0 } || _lastTaskItems == null)
            {
                TaskList.ItemsSource = null;
                _lastTaskItems = new List<Avalonia.Controls.Control>();
            }

            TaskList.IsVisible = false;
            NoneBox.IsVisible = true;
            TaskInfoText.IsVisible = false;
        }
        else
        {
            TaskList.IsVisible = true;
            NoneBox.IsVisible = false;
            TaskInfoText.IsVisible = true;
            TaskInfoText.Text = string.Format(I18n["MainWindow.Task.CountInfo"], tasks.Count);

            var visible = new List<Avalonia.Controls.Control>(tasks.Count);
            foreach (var task in tasks)
            {
                if (task.Item == null) continue;
                task.Item.Margin = new Thickness(5);
                visible.Add(task.Item);
            }

            // 集合未发生变化时跳过重建：
            // 该方法每 30 秒被定时调用一次，无条件 ItemsSource=null + 重新赋值
            // 会导致整个任务列表被拆除并重建，即使内容完全没变。
            if (IsSameTaskItems(_lastTaskItems, visible)) return;

            TaskList.ItemsSource = null;
            TaskList.ItemsSource = visible;
            _lastTaskItems = visible;
        }
    }

    /// <summary>按引用逐项比较两个控件集合是否等价</summary>
    private static bool IsSameTaskItems(
        List<Avalonia.Controls.Control>? a,
        List<Avalonia.Controls.Control> b)
    {
        if (a == null || a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (!ReferenceEquals(a[i], b[i])) return false;
        return true;
    }

    public void SetReboot()
    {
        RebootBtn.IsVisible = true;
    }

    private void RebootBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) startInfo.Verb = "open";

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
        Environment.Exit(0);
    }

    #endregion


    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty || e.Property == TitleProperty)
            RefreshWindowChrome();
    }

    private void OnConfigAfterSave(object? sender, EventArgs e)
    {
        // 防抖：滚轮/拖动等高频保存会短时间内触发数十次 RefreshWindowChrome，
        // 合并到 120ms 一次，避免动画期间被频繁打断。
        if (_configRefreshDebounce == null)
        {
            _configRefreshDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _configRefreshDebounce.Tick += (_, _) =>
            {
                _configRefreshDebounce!.Stop();
                RefreshWindowChrome();
            };
        }

        _configRefreshDebounce.Stop();
        _configRefreshDebounce.Start();
    }

    private void ScheduleConfigSave()
    {
        if (_configSaveDebounce == null)
        {
            _configSaveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _configSaveDebounce.Tick += (_, _) =>
            {
                _configSaveDebounce!.Stop();
                Core.Global.GlobalModel.Config.Save();
            };
        }

        _configSaveDebounce.Stop();
        _configSaveDebounce.Start();
    }

    private void RefreshWindowChrome()
    {
        var useSystemWindow = Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        var isBlurStyle = Core.Global.GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Blur;
        var currentState = WindowState;

        if (useSystemWindow != _lastUseSystemWindow)
        {
            _lastUseSystemWindow = useSystemWindow;
            MaxBtn.IsVisible = !useSystemWindow;
            MinBtn.IsVisible = !useSystemWindow;
            CloseBtn.IsVisible = !useSystemWindow;
            ExtendClientAreaToDecorationsHint = !useSystemWindow;
            ExtendClientAreaTitleBarHeightHint = -1;
        }

        /*if (OperatingSystem.IsWindows())
        {
            var newPadding = currentState == WindowState.Maximized && !useSystemWindow
                ? new Thickness(8)
                : new Thickness(0);
            if (Padding != newPadding) Padding = newPadding;
        }*/

        if (currentState != _lastWindowState)
        {
            _lastWindowState = currentState;
            var newGlyph = currentState == WindowState.Maximized ? "\uE923" : "\uE922";
            if (MaxBtnIcon.Glyph != newGlyph) MaxBtnIcon.Glyph = newGlyph;
        }

        if (_lastIsBlurStyle != isBlurStyle)
        {
            _lastIsBlurStyle = isBlurStyle;
            BackgroundCover.IsVisible = isBlurStyle;
        }

        if (_lastTitle != Title)
        {
            _lastTitle = Title;
            if (TitleBlock.Text != Title) TitleBlock.Text = Title ?? "";
        }
    }

    /// <summary>
    /// 唤醒音量提示框
    /// </summary>
    public void ShowVolumeCard()
    {
        // 确保在 UI 线程执行
        Dispatcher.UIThread.Post(() =>
        {
            _volumeControlTimer.Stop();
            MediaVolumeCard.Margin = new Thickness(0, 19, 0, 0);

            _volumeControlTimer.Start();
        });
    }

    private void VolumeControlTimer_Tick(object? sender, EventArgs e)
    {
        // 时间到了，缩回顶部
        MediaVolumeCard.Margin = new Thickness(0, -76, 0, 0);
        _volumeControlTimer.Stop();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) _ctrlPressed = true;
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) _ctrlPressed = false;
    }

    private void OnWindowDeactivated(object sender, EventArgs e)
    {
        _ctrlPressed = false;
    }

    private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (_ctrlPressed)
        {
            ShowVolumeCard();

            var delta = e.Delta.Y;
            var step = 0.05;

            var newVolume = MediaManager.Instance.Volume + (delta > 0 ? step : -step);
            if (newVolume * 100 < 0)
                newVolume = 0;
            else if (newVolume * 100 > 100) newVolume = 1;

            MediaVolume.Value = newVolume * 100;

            if (MediaVolume.Value != 0)
                MediaVolumeCard.Width = 170;
            else
                MediaVolumeCard.Width = 150;

            DisableVolumeText.IsVisible = false;

            switch (MediaVolume.Value)
            {
                case <= 0:
                    MediaVolumeIcon.Glyph = "\uE74F";
                    DisableVolumeText.IsVisible = true;
                    break;
                case < 33:
                    MediaVolumeIcon.Glyph = "\uE993";
                    break;
                case < 66:
                    MediaVolumeIcon.Glyph = "\uE994";
                    break;
                case < 100:
                    MediaVolumeIcon.Glyph = "\uE995";
                    break;
            }

            Console.WriteLine($@"当前音量：{(int)(newVolume * 100)}%");

            // 应用新音量
            Core.Global.GlobalModel.Config.Data.MediaVolume = newVolume;
            // 防抖保存：滚轮短时间会触发数十次 Config.Save()，
            // 这中间会做 JsonSerializer + File.WriteAllText，阻塞 UI 线程。
            // 用 200ms 防抖合并写入，避免动画期间被频繁打断。
            ScheduleConfigSave();

            MediaManager.Instance.Volume = (float)Math.Clamp(Core.Global.GlobalModel.Config.Data.MediaVolume, 0.0, 1.0);

            // 阻止事件继续冒泡
            e.Handled = true;
        }
    }

    public NoticePanel Notice => NoticePanel;
    private bool _isMainWindow { get; set; }
    private bool _isMinBtn { get; set; } = true;
    private bool _isMaxBtn { get; set; } = true;

    public bool IsTaskCardOpen { get; private set; }

    public void UpdateWindowBorder()
    {
        MaxBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        MinBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        CloseBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        // Avalonia 12: WindowDecorations replaces ExtendClientAreaChromeHints.
        WindowDecorations = Core.Global.GlobalModel.Config.Data.IsUseSystemWindow
            ? WindowDecorations.Full
            : WindowDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        ExtendClientAreaTitleBarHeightHint = -1;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void MinBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaxBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();

        Environment.Exit(0);
    }

    public void CloseDraw()
    {
        SetBorderState(false);
    }

    public async void OpenDraw(object? page, string title)
    {
        BorderTitle.Text = title;
        await SetBorderState(true);

        Frame.NavigateTo(page);
    }

    private async Task SetBorderState(bool state)
    {
        if (state)
        {
            BottomBorder.Margin = new Thickness(DrawMarginLR, Height, DrawMarginLR, -Height);
            await Task.Delay(100);
            BorderGrid.IsVisible = true;
            BottomBorder.Margin = new Thickness(DrawMarginLR, 76, DrawMarginLR, 0);
            BorderBackground.Opacity = 0.3;
            await Task.Delay(200);
        }
        else
        {
            BottomBorder.Margin = new Thickness(DrawMarginLR, Height, DrawMarginLR, -Height);
            BorderBackground.Opacity = 0;
            await Task.Delay(800);
            BorderGrid.IsVisible = false;
            Frame.NavigateTo("");
        }
    }

    private void CloseBorderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SetBorderState(false);
    }

    public void SetBlurState(bool state)
    {
        // 关键：XAML 中 ContentView/BackgroundGroupBox 配置了 EffectTransition，
        // 它监听的是 Effect 属性本身变化（oldEffect -> newEffect 之间的交叉淡化），
        // 所以必须"整体赋值"一个 BlurEffect，而不是修改现有实例的 Radius，
        // 否则 Effect 属性没变，过渡不触发、变瞬时切换。
        // BlurEffect 对象本身只是轻量描述符（Radius/...），真正贵的是离屏 layer，
        // 而离屏 layer 由渲染系统管理，与 new 几次 BlurEffect 无关。
        //
        // 注意：模糊结束后必须把 Effect 置回 null。只要 Effect 非 null，
        // 即便 Radius 为 0，渲染系统仍会为整棵内容树分配离屏渲染目标并逐帧合成。
        if (state)
        {
            // 递增 token，作废尚未执行的清理任务
            _blurClearToken++;
            ContentView.Effect = new BlurEffect { Radius = 50 };
            BackgroundGroupBox.Effect = new BlurEffect { Radius = 50 };
            BackgroundGroupBox.Margin = new Thickness(-50);
        }
        else
        {
            ContentView.Effect = new BlurEffect { Radius = 0 };
            BackgroundGroupBox.Effect = new BlurEffect { Radius = 0 };
            BackgroundGroupBox.Margin = new Thickness(0);

            // 等过渡动画播完再彻底移除 Effect，避免常驻离屏合成
            _ = ClearBlurEffectAfterTransitionAsync();
        }
    }

    /// <summary>
    /// 淡出动画结束后移除 Effect，消除非模糊状态下的离屏渲染开销。
    /// </summary>
    private async Task ClearBlurEffectAfterTransitionAsync()
    {
        var token = ++_blurClearToken;

        // 略长于 XAML 中 EffectTransition 的 0.58s
        await Task.Delay(650);

        // 期间又被重新打开模糊则放弃本次清理（SetBlurState 会递增 token）
        if (token != _blurClearToken) return;

        if (ContentView.Effect is BlurEffect { Radius: <= 0 }) ContentView.Effect = null;
        if (BackgroundGroupBox.Effect is BlurEffect { Radius: <= 0 }) BackgroundGroupBox.Effect = null;
    }

    private int _blurClearToken;

    public async void OpenTaskCard()
    {
        SetBlurState(true);
        TaskCard.Margin = new Thickness(10);
        IsTaskCardOpen = true;
        BlackView.IsVisible = true;

        DropBox.Opacity = 0;
        await Task.Delay(360);
        DropBox.IsVisible = false;
    }

    public void CloseTaskCard()
    {
        SetBlurState(false);
        TaskCard.Margin = new Thickness(500, 10, -500, 10);
        IsTaskCardOpen = false;
        BlackView.IsVisible = false;
    }

    private void BlackView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CloseTaskCard();
    }
}