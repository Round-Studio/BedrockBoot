using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    private CancellationTokenSource _refreshCancellationTokenSource;
    private DispatcherTimer _refreshTimer;

    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;

#if RELEASE
        GameControls.IsEnabled = GlobalModel.FunctionOption.IsEnableGameInstanceControl;
#endif

#if LINUX
        Mods.IsVisible = false;
        Plugin.IsVisible = false;
#endif
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; }

    public void Update()
    {
        IsEditMode = false;

        var image = "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png";
        if (VersionInfo.Info.VersionType != BedrockLauncher.Core.MinecraftGameTypeVersion.Release)
            image = "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";

        IconBox.Background = new ImageBrush
        {
            Source = GetImage(image)
        };

        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
        VersionName.Text = VersionInfo.Info.VersionName;
        VersionReady.Text =
            $"{VersionInfo.Info.Version} · {VersionInfo.Info.VersionType} · {VersionInfo.Info.BuildType}";

        StartPlayTimeRefresh();

        IsEditMode = true;
    }

    public Bitmap GetImage(string url)
    {
        var uri = new Uri(url);

        using (var stream = AssetLoader.Open(uri))
        {
            return new Bitmap(stream);
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
                    TimeSpan totalTime = TimeSpan.FromSeconds(playerData.TotalPlayTime);

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
        if (VersionInfo != null)
        {
            StartPlayTimeRefresh();
        }
    }

    // 当控件卸载时（视图消失）- 修正为正确的签名
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        // 停止定时刷新
        StopPlayTimeRefresh();
        base.OnUnloaded(e);
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((TabItem)InstanceTabControl.SelectedItem).Tag.ToString();

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
                    break;
                case "Mods":
                    InstanceFrame.NavigateTo(new InstanceMods(VersionInfo));
                    break;
                case "Pack":
                    InstanceFrame.NavigateTo(new InstancePack(VersionInfo));
                    break;
                case "Save":
                    InstanceFrame.NavigateTo(new InstanceSave(VersionInfo));
                    break;
                case "Screenshots":
                    InstanceFrame.NavigateTo(new InstanceScreenshots(VersionInfo));
                    break;
                case "Server":
                    InstanceFrame.NavigateTo(new InstanceServer(VersionInfo));
                    break;
                case "Plugin":
                    InstanceFrame.NavigateTo(new InstancePlugins(VersionInfo));
                    break;
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls(VersionInfo));
                    break;
            }
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