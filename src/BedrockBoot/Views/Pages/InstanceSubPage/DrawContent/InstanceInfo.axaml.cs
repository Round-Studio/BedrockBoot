using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceInfo : UserControl
{
    public InstanceInfo()
    {
        IsEdit = false;

        InitializeComponent();

#if LINUX
        IsolationCard.IsVisible = false;
        HighLevel.IsVisible = false;
#endif

#if RELEASE
        InstanceMod.IsVisible = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableGameInstanceMods;
#endif
    }

    public InstanceInfo(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        UpdateUI();
    }

    public bool IsEdit { get; set; }
    public VersionConfig VersionInfo { get; set; }
    private CancellationTokenSource _refreshCancellationTokenSource;
    private DispatcherTimer _refreshTimer;

    public async Task UpdateUI()
    {
        var image = "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png";
        if (VersionInfo.Info.VersionType != MinecraftGameTypeVersion.Release)
            image = "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";
        IconBox.Background = new ImageBrush
        {
            Source = await ImageLoader.LoadIconAsync(image)
        };
        VersionName.Text = VersionInfo.Info.VersionName;
        VersionReady.Text =
            $"{VersionInfo.Info.Version} · {VersionInfo.Info.VersionType} · {VersionInfo.Info.BuildType}";
        
        StartPlayTimeRefresh();
        
        Task.Run(() =>
        {
            IsEdit = false;

            Dispatcher.UIThread.Invoke(() =>
            {
                InstanceName.Text = VersionInfo.Info.VersionName;

                if (VersionInfo.Config == null)
                    VersionInfo.Config = new VersionConfig.VersionConfigEntry();

                InstanceArgs.Text = VersionInfo.Config.OtherCommand;
                InstanceConsole.IsChecked = VersionInfo.Config.IsConsole;
                InstanceEdit.IsChecked = VersionInfo.Config.IsEditModel;
                InstanceMod.IsChecked = VersionInfo.Config.IsModes;
                InstanceIsolated.IsChecked = VersionInfo.Config.IsVersionIsolated;
                InstanceDetailedLogs.IsChecked = VersionInfo.Config.IsDetailedLog;
            });

            Thread.Sleep(500);
            IsEdit = true;
        });
    }

    private void TextTypeConfig_OnChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            if (string.IsNullOrEmpty(InstanceName.Text))
                VersionInfo.Info.VersionName = Path.GetFileName(VersionInfo.VersionPath);
            else VersionInfo.Info.VersionName = InstanceName.Text;

            VersionInfo.Config.OtherCommand = InstanceArgs.Text;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void StartPlayTimeRefresh()
    {
        StopPlayTimeRefresh();

        _refreshCancellationTokenSource = new CancellationTokenSource();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _refreshTimer.Tick += async (sender, e) => await RefreshPlayTimeAsync();
        _refreshTimer.Start();

        Dispatcher.UIThread.Post(async () => await RefreshPlayTimeAsync());
    }

    private void StopPlayTimeRefresh()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= async (sender, e) => await RefreshPlayTimeAsync();
            _refreshTimer = null;
        }

        if (_refreshCancellationTokenSource != null)
        {
            _refreshCancellationTokenSource.Cancel();
            _refreshCancellationTokenSource.Dispose();
            _refreshCancellationTokenSource = null;
        }
    }

    private async Task RefreshPlayTimeAsync()
    {
        try
        {
            VersionInfo = GameInfoHelper.GetVersionConfig(VersionInfo.VersionPath);
            if (VersionInfo == null ||
                VersionInfo?.PlayerData == null)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (TotalDuration != null)
                {
                    var playerData = VersionInfo.PlayerData;

                    // 获取总游玩时间（秒）并转换为 TimeSpan
                    var totalTime = TimeSpan.FromSeconds(playerData.TotalPlayTime);

                    TotalDuration.Text =
                        string.Format(I18nManager.Instance["Draw.Instance.TotalTime"],
                            totalTime.TotalHours.ToString("F2"));
                }
            });
        }
        catch (OperationCanceledException)
        {
            // 取消操作时忽略
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"刷新游玩时间失败: {ex.Message}");
            StopPlayTimeRefresh();
        }
    }

    // 当控件加载完成时
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // 确保定时器在控件加载时启动
        if (VersionInfo != null) StartPlayTimeRefresh();
    }

    // 当控件卸载时（视图消失）- 修正为正确的签名
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        // 停止定时刷新
        StopPlayTimeRefresh();
        base.OnUnloaded(e);
    }

    private void BoolTypeConfig_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.IsConsole = (bool)InstanceConsole.IsChecked!;
            VersionInfo.Config.IsEditModel = (bool)InstanceEdit.IsChecked!;
            VersionInfo.Config.IsVersionIsolated = (bool)InstanceIsolated.IsChecked!;
            VersionInfo.Config.IsModes = (bool)InstanceMod.IsChecked!;
            VersionInfo.Config.IsDetailedLog = (bool)InstanceDetailedLogs.IsChecked!;

            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskLaunchGameItem.Launch(VersionInfo);
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(VersionInfo.VersionPath);
    }
}