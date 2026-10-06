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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Standard.Entity.Manifest;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Standard.Helper;
using BedrockBoot.Entity;
using BedrockBoot.Models;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Media;
using BedrockBoot.Models.Native;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Models.Pack.System.DropFile;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Models.Style;
using BedrockBoot.Service;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Service.Protocol.Routes;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Standard.Interface.Platform.Game;
using BedrockBoot.Style.Controls;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.SetupPage;
using BedrockBoot.Views.Windows.SubWindows;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Notice.Info;
using Round.SDK.Helper;
using Wallpaper.Avalonia.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ILauncher = BedrockBoot.Standard.Interface.Platform.Game.ILauncher;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : Window
{
    #region 字段

    private static List<string> _installedFontNames;
    private ImageLoader _imageLoader = new();

    private bool _ctrlPressed = false;
    public int DrawMarginLR = 10;
    private DispatcherTimer _volumeControlTimer;
    private DispatcherTimer _configRefreshDebounce;
    private DispatcherTimer _configSaveDebounce;

    private WindowState _lastWindowState;
    private bool? _lastUseSystemWindow;
    private bool? _lastIsBlurStyle;
    private string? _lastTitle;

    private CancellationTokenSource? _accountPanelCloseCts;
    private bool _accountPanelAnimating;

    private bool _isMainWindow { get; set; }
    private bool _isMinBtn { get; set; } = true;
    private bool _isMaxBtn { get; set; } = true;

    private TaskCompletionSource<MsUserConfig>? _chooseAccountTcs;

#if WINDOWS
    private IntPtr _windowHandle;
    private double _lastReportedProgress = -1;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(100);
    private const double MinProgressDelta = 1;
#endif

    #endregion

    #region 属性

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

    private I18nManager I18n => I18nManager.Instance;
    public bool IsWindowActive => IsActive;
    private DesktopThumbnailWindow? DesktopThumbnailWindow { get; set; }
    public NoticePanel Notice => NoticePanel;
    public bool IsTaskCardOpen { get; private set; }

    #endregion

    #region 构造函数

    public MainWindow()
    {
        InitializeComponent();
        DialogHost.SetHost(DialogHost);
        UpdateZoom();
        GlobalModel.MainWindow = this;

        if (!Core.Global.GlobalModel.Config.Data.IsFirstRun)
            MainFrame.NavigateTo(new MainPage());
        else
            Loaded += (_, _) => MainFrame.NavigateTo(new SetupRoot());
        InitializeWindowBounds();

        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        ILauncher.LaunchedBehavior = () => Dispatcher.UIThread.Invoke(RunBehavior);

        SetupDynamicHotkey();
        _ = InitializeAsync();
        _ = UpdateAccount();

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

        PropertyChanged += OnSelfPropertyChanged;
        Core.Global.GlobalModel.Config.AfterSave += OnConfigAfterSave;

        Closed += (_, _) =>
        {
            Core.Global.GlobalModel.Config.AfterSave -= OnConfigAfterSave;
            PropertyChanged -= OnSelfPropertyChanged;
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
                    var item = new MenuItem
                    {
                        Header = link.Name,
                        Icon = new FontIcon
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

        PluginLoader.OnPluginLoadException = i =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ErrorBtn.IsVisible = true;
                ErrorBtnText.Text = $"{i} 个内部错误";
            });
        };
    }

    #endregion

    #region 公共方法

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

    public void UpdateLiveOpacity()
    {
        LiveOpacity.Opacity =
            (100 - Core.Global.GlobalModel.Config.Data.StyleConfig.LiveOpacity) * 0.01;
    }

    public void ReSetBackground()
    {
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        BackgroundView.ReleaseBackground();
        BackgroundView.IsVisible = false;
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
                    BackgroundView.ApplyImageBackground(new StyleConfig
                    {
                        Background3D = packConfig.BackgroundUse3D,
                        BackgroundImage = packConfig.BackgroundImageFileName,
                        BackgroundImageOpacity = packConfig.BackgroundImageOpacity,
                        BackgroundImageBlur = packConfig.BackgroundImageBlur,
                        BackgroundAnimation = packConfig.BackgroundAnimation,
                        StyleType = StyleType.Image
                    });
                    BackgroundView.IsVisible = true;

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

    public void InitRefreshTaskItemTask()
    {
        Task.Run(() =>
        {
            while (true)
            {
                Dispatcher.UIThread.Invoke(UpdateTaskUI);
                Thread.Sleep(30000);
            }
        });
    }

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

            var visible = new List<Avalonia.Controls.Control>(tasks.Count);
            foreach (var task in tasks)
            {
                if (task.Item == null) continue;
                task.Item.Margin = new Thickness(5);
                visible.Add(task.Item);
            }

            TaskList.ItemsSource = visible;
        }
    }

    public void SetReboot()
    {
        RebootBtn.IsVisible = true;
    }

    public void ShowVolumeCard()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _volumeControlTimer.Stop();
            MediaVolumeCard.Margin = new Thickness(0, 19, 0, 0);

            _volumeControlTimer.Start();
        });
    }

    public void UpdateWindowBorder()
    {
        MaxBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        MinBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        CloseBtn.IsVisible = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        WindowDecorations = Core.Global.GlobalModel.Config.Data.IsUseSystemWindow
            ? WindowDecorations.Full
            : WindowDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = !Core.Global.GlobalModel.Config.Data.IsUseSystemWindow;
        ExtendClientAreaTitleBarHeightHint = -1;
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

    public void SetBlurState(bool state)
    {
        ContentView.Effect = new BlurEffect { Radius = state ? 50 : 0 };
        BackgroundGroupBox.Effect = new BlurEffect { Radius = state ? 50 : 0 };
        BackgroundGroupBox.Margin = new Thickness(state ? -50 : 0);
    }

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

    public async Task<MsUserConfig?> ChooseAccount()
    {
        _chooseAccountTcs = new TaskCompletionSource<MsUserConfig>();

        Dispatcher.UIThread.Invoke(() =>
        {
            PART_AccountList.Children.Clear();
            if (MsAccountManager.Accounts.Accounts.Count <= 0)
            {
                Notice.AddNotice(new NoticeInfo
                {
                    Title = "无账户",
                    Message = "未登录任何账户"
                });
                _chooseAccountTcs?.TrySetResult(null);
                return;
            }

            MsAccountManager.Accounts.Accounts.ForEach(user =>
            {
                var btn = new AccountButton
                {
                    HeaderImageUrl = user.UserIconUrl,
                    AccountName = user.UserName
                };
                btn.Click += (_, _) =>
                {
                    _chooseAccountTcs?.TrySetResult(user);
                    AccountChoose_Show(false);
                };
                PART_AccountList.Children.Add(btn);
            });
            AccountChoose_Show(true);
        });

        return await _chooseAccountTcs.Task;
    }

    public void UpdateZoom(bool isSetting = false)
    {
        var zoom = UIZoomExtensions.ToScale(Core.Global.GlobalModel.Config.Data.UIZoom);
        MainLayoutTransformControl.LayoutTransform = new ScaleTransform(zoom, zoom);
    }

    public async Task UpdateAccount(bool b = false)
    {
        AccountBtn.IsVisible = Core.Global.GlobalModel.Config.Data.IsUseMultipleUsers;
        if (Core.Global.GlobalModel.Config.Data.IsUseMultipleUsers)
        {
            var users = MsAccountManager.Accounts?.Accounts;
            var selIndex = users.FindLastIndex(user => user.BUID == MsAccountManager.Accounts?.SelectUserBUID);
            AccountHeaderImage.Background = new ImageBrush
            {
                Source = await _imageLoader.LoadIconAsync(users[selIndex].UserIconUrl)
            };
            AccountName.Text = users[selIndex].UserName;
        }

        if (!b) await AccountPanelControl.UpdateAccount();
    }

    #endregion

    #region 私有方法 - 初始化

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

        Dispatcher.UIThread.Invoke(() => { UpdateTheme(); });

        CoreInitialize.Init();

        Dispatcher.UIThread.Invoke(() => { LoadBox.IsVisible = false; });
    }

    private void SetupDynamicHotkey()
    {
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void UpdateBack()
    {
        var style = Core.Global.GlobalModel.Config.Data.StyleConfig;

#if WINDOWS
        var handle = TryGetPlatformHandle();
        var hwnd = handle.Handle;
#endif

        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

        switch (style.StyleType)
        {
#if WINDOWS
            case StyleType.Mica:
                var micaType = style.MicaType == MicaType.MicaAlt
                    ? NativeDwmApi.DwmSystemBackdropType.MicaAlt
                    : NativeDwmApi.DwmSystemBackdropType.MicaBase;
                NativeDwmApi.SetBackdrop(hwnd, micaType);
                break;
            case StyleType.Blur:
                NativeDwmApi.SetBackdrop(hwnd, NativeDwmApi.DwmSystemBackdropType.Acrylic);
                break;
#endif
            case StyleType.Image:
                BackgroundView.ApplyImageBackground(style);
                BackgroundView.IsVisible = true;
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
#if WINDOWS
                NativeDwmApi.SetBackdrop(hwnd, NativeDwmApi.DwmSystemBackdropType.None);
                if (DesktopThumbnailWindow == null) DesktopThumbnailWindow = new DesktopThumbnailWindow();
                if (style.LiveBlur) NativeDwmApi.SetBackdrop(hwnd, NativeDwmApi.DwmSystemBackdropType.Acrylic);

                DesktopThumbnailWindow?.ShowBelow(this);
                LiveOpacity.IsVisible = true;
                UpdateLiveOpacity();
#endif
                break;
            case StyleType.Default:
                BackgroundView.ApplyImageBackground(style);
                BackgroundView.IsVisible = true;
                break;
        }
    }

#if WINDOWS
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

    #endregion

    #region 私有方法 - 窗口事件

    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty || e.Property == TitleProperty)
            RefreshWindowChrome();
    }

    private void OnConfigAfterSave(object? sender, EventArgs e)
    {
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

        if (currentState != _lastWindowState)
        {
            _lastWindowState = currentState;
            var newGlyph = currentState == WindowState.Maximized ? "\uE923" : "\uE922";
            if (MaxBtnIcon.Glyph != newGlyph) MaxBtnIcon.Glyph = newGlyph;
        }

        _lastIsBlurStyle = isBlurStyle;
    }

    private void VolumeControlTimer_Tick(object? sender, EventArgs e)
    {
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

            Core.Global.GlobalModel.Config.Data.MediaVolume = newVolume;
            ScheduleConfigSave();

            MediaManager.Instance.Volume = (float)Math.Clamp(Core.Global.GlobalModel.Config.Data.MediaVolume, 0.0, 1.0);

            e.Handled = true;
        }
    }

    private async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
            if (e.Source is not (TextBox or TextBlock))
            {
                CopyService.HandleCopyAction();
                e.Handled = true;
            }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
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

    #region 私有方法 - 按钮事件

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

    private void TaskBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsTaskCardOpen) CloseTaskCard();
        else OpenTaskCard();
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

    private void CloseBorderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SetBorderState(false);
    }

    private void BlackView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CloseTaskCard();
    }

    private void ErrorBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        foreach (var ex in PluginLoader.PluginLoadExceptions)
        {
            sb.AppendLine($"{ex}");
            sb.AppendLine();
        }

        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"加载插件发生了错误，共发生 {PluginLoader.PluginLoadExceptions.Count} 个错误：",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBox
        {
            Text = sb.ToString().TrimEnd(),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            MaxHeight = 300,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "通常您需要检查插件是否与当前启动器版本兼容，或者尝试更新插件。",
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "如还是无效，请尝试联系插件开发者或启动器开发者。",
            TextWrapping = TextWrapping.Wrap
        });

        DialogHost.Show(new DialogInfo
        {
            Title = $"{PluginLoader.PluginLoadExceptions.Count} 个内部错误",
            Content = panel,
            CloseButtonText = "确定"
        });
    }

    private async void AccountBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _accountPanelCloseCts?.Cancel();

        if (AccountPanel.IsVisible && AccountPanel.Opacity > 0)
        {
            HideAccountPanel();
            return;
        }

        await ShowAccountPanelAsync();
    }

    private void AccountPanel_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _accountPanelCloseCts?.Cancel();
    }

    private async void AccountPanel_OnPointerExited(object? sender, PointerEventArgs e)
    {
        _accountPanelCloseCts?.Cancel();
        _accountPanelCloseCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(120, _accountPanelCloseCts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (!AccountPanel.IsPointerOver && !AccountBtn.IsPointerOver) HideAccountPanel();
    }

    #endregion

    #region 私有方法 - 拖拽

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
        if (!DropBox.IsVisible) return;

        DropBox.Opacity = 0;
        SetBlurState(false);
        await Task.Delay(360);
        DropBox.IsVisible = false;
    }

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

        var handler = new DropFileHandler(paths.ToList());
        handler.Handle();
    }

    #endregion

    #region 私有方法 - 抽屉与卡片

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

    #endregion

    #region 私有方法 - 账户选择面板

    private async Task AccountChoose_Show(bool show)
    {
        if (show)
        {
            PART_ChooseAccount.IsVisible = true;
            PART_AccountChooseBackground.Opacity = 0.7;
            await Task.Delay(200);
            PART_AccountChooseBorderCard.Margin = new Thickness(0);
        }
        else
        {
            PART_AccountChooseBorderCard.Margin = new Thickness(0, 260, 0, -260);
            await Task.Delay(200);
            PART_AccountChooseBackground.Opacity = 0;
            await Task.Delay(820);
            PART_ChooseAccount.IsVisible = false;
        }
    }

    private void PART_AccountChooseBackground_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AccountChoose_Show(false);
        _chooseAccountTcs?.TrySetResult(null!);
    }

    private async Task ShowAccountPanelAsync()
    {
        if (_accountPanelAnimating) return;
        _accountPanelAnimating = true;

        AccountPanel.RenderTransform = TransformOperations.Parse("scale(0)");
        AccountPanel.Opacity = 0;
        AccountPanel.IsVisible = true;

        await Task.Delay(20);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        AccountPanel.RenderTransform = TransformOperations.Parse("scale(1)");
        AccountPanel.Opacity = 1;

        _accountPanelAnimating = false;
    }

    private void HideAccountPanel()
    {
        AccountPanel.RenderTransform = TransformOperations.Parse("scale(0)");
        AccountPanel.Opacity = 0;

        _accountPanelCloseCts?.Cancel();
        _accountPanelCloseCts = new CancellationTokenSource();
        var token = _accountPanelCloseCts.Token;

        Task.Delay(220, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (AccountPanel.Opacity < 0.01)
                    AccountPanel.IsVisible = false;
            });
        }, token);
    }

    #endregion
}