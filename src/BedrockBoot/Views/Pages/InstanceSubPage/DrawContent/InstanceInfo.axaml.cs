using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Base.Helper;
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

    private bool _isEditGameIcon = true;

    public async Task UpdateUI()
    {
        UpdateImage();
        VersionName.Text = VersionInfo.Info.VersionName;
        VersionReady.Text =
            $"{VersionInfo.Info.Version} · {VersionInfo.Info.VersionType} · {VersionInfo.Info.BuildType}";
        CustomizationBox.IsEnabled = VersionInfo.Info.GameIconType == GameIconType.Customization;
        IconPathInput.Text = VersionInfo.Info.GameIconPath;
        if ((int)VersionInfo.Info.GameIconType >= 2025)
        {
            GameIconCard.IsVisible = false;
            _isEditGameIcon = false;
        }
        else
        {
            GameIconSel.SelectedIndex = (int)VersionInfo.Info.GameIconType;
        }
        
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
                CatalogStrategy.SelectedIndex = (int)VersionInfo.Config.IsolationFolderPolicy;
            });

            Thread.Sleep(500);
            IsEdit = true;
        });
    }

    private async Task UpdateImage()
    {
        var image = "avares://BedrockBoot/Assets/Image/world-preview-flat-fixed-pixels.png";

        if (!string.IsNullOrEmpty(VersionInfo.Info.CoverImage))
            if (File.Exists(VersionInfo.Info.CoverImage))
                image = VersionInfo.Info.CoverImage;

        IconBox.Update(image);
        
        GameIcon.Source = await ImageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(VersionInfo));
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
            VersionName.Text = VersionInfo.Info.VersionName;
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

    private void ResetImageBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        VersionInfo.Info.CoverImage = null;
        GameInfoHelper.SaveVersionConfig(VersionInfo);
        UpdateImage();
    }

    private async void ChooseNewImageBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png" }
                }
            }
        });

        if (files.Count > 0)
        {
            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            VersionInfo.Info.CoverImage = filePath;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
            UpdateImage();
        }
    }

    private void CatalogStrategy_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.IsolationFolderPolicy = (CatalogStrategyEnum)CatalogStrategy.SelectedIndex;
            VersionInfo.Config.FolderPolicyStr =
                IsolationPolicyHelper.ParsePolicyConfig(VersionInfo.Config.IsolationFolderPolicy);
            
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void GameIconSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit && _isEditGameIcon)
        {
            VersionInfo.Info.GameIconType = (GameIconType)GameIconSel.SelectedIndex;

            GameInfoHelper.SaveVersionConfig(VersionInfo);
            UpdateUI();
        }
    }

    private async void IconPathChooseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await TopLevel.GetTopLevel(this).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                }
            }
        });

        var file = files.FirstOrDefault()?.Path.AbsolutePath;
        VersionInfo.Info.GameIconPath = file;
        GameInfoHelper.SaveVersionConfig(VersionInfo);

        UpdateUI();
    }
}