using System;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadGameItem : UserControl
{
    private EasyDownload _downloader;

    public TaskDownloadGameItem()
    {
        InitializeComponent();
    }

    public TaskDownloadGameItem(BuildInfo info, string url, bool? isUsePack, string dir, string gameName) : this()
    {
        BuildInfo = info;
        InstallFolder = dir;
        GameName = gameName;
        Url = url;
        IsUsePack = (bool)isUsePack!;

        InitializeDownloader();
    }

    public string InstallFolder { get; set; }
    public string GameName { get; set; }
    public string Url { get; set; }
    public bool IsUsePack { get; set; }
    public BuildInfo BuildInfo { get; set; }

    private void InitializeDownloader()
    {
        _downloader = new EasyDownload(BuildInfo, IsUsePack, InstallFolder, GameName)
        {
            // 修改为使用整合的DownloadProgressInfo
            DownloadProgress = (text, progressInfo) =>
                Dispatcher.UIThread.Invoke(() => UpdateDownloadProgress(text, progressInfo)),

            // 移除单独的DownloadSpeed回调
            // DownloadSpeed = speed =>
            //     Dispatcher.UIThread.Invoke(() => MainSpeedText.Text = speed),

            // 新增合并进度回调
            MergeProgress = (text, percentage) =>
                Dispatcher.UIThread.Invoke(() => UpdateMergeProgress(text, percentage)),

            ExtractionProgress = (text, percentage) =>
                Dispatcher.UIThread.Invoke(() => UpdateExtractionProgress(text, percentage)),

            DeploymentProgress = (text, progress) =>
                Dispatcher.UIThread.Invoke(() => UpdateDeploymentProgress(text, progress)),

            StatusText = text =>
                Dispatcher.UIThread.Invoke(() => MainText.Text = text),

            InstallStateChanged = states =>
                Dispatcher.UIThread.Invoke(() => HandleInstallState(states)),

            ErrorOccurred = (title, message, ex) =>
                Dispatcher.UIThread.Invoke(() => ShowErrorDialog(title, message, ex)),
                
            Completed = (c) =>
                Dispatcher.UIThread.Invoke(() => MainSpeedText.Text = "下载完成")
        };
    }

    private void UpdateMergeProgress(string text, double percentage)
    {
        InsMergeBar.IsIndeterminate = false;
        InsMergeBar.Value = percentage;
        MainText.Text = text;
    }

    public void Install(Action installed)
    {
        CardTitle.Text = $"下载游戏 {GameName} [{BuildInfo.ID}]";

        InsGetUrlBar.IsIndeterminate = true;
        if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            InsInstallGamePanel.IsVisible = false;

        Task.Run(async () =>
        {
            await _downloader.InstallAsync(Url);
            installed?.Invoke();
        });
    }

    // 修改：使用DownloadProgressInfo参数
    private void UpdateDownloadProgress(string text, DownloadProgressInfo progressInfo)
    {
        if (InsGetUrlBar.IsIndeterminate)
        {
            InsGetUrlBar.IsIndeterminate = false;
            InsGetUrlBar.Value = 100;
        }

        // 更新进度条
        InsDownGameBar.Value = progressInfo.Percentage;
        
        // 更新主文本
        MainText.Text = text;
        
        // 更新速度文本 - 从progressInfo中获取
        if (!string.IsNullOrEmpty(progressInfo.Speed))
        {
            MainSpeedText.Text = $"{progressInfo.Speed}/s";
        }
        
        // 可选：显示详细的下载信息
        if (progressInfo.TotalBytes > 0)
        {
            // 显示已下载/总大小
            var downloaded = FormatBytes(progressInfo.DownloadedBytes);
            var total = FormatBytes(progressInfo.TotalBytes);
            // 可以将这些信息显示在UI的其他位置，如果需要的话
        }
    }

    private void UpdateExtractionProgress(string text, double percentage)
    {
        InsUnZipBar.IsIndeterminate = false;
        InsUnZipBar.Value = percentage;
        MainText.Text = text;
    }

    private void UpdateDeploymentProgress(string text, DeploymentProgress progress)
    {
        InsInstallGameBar.Value = progress.percentage;
        MainText.Text = text;
    }

    private void HandleInstallState(InstallStates states)
    {
        switch (states)
        {
            case InstallStates.Extracting:
                InsUnZipBar.IsIndeterminate = false;
                break;

            case InstallStates.Extracted:
                InsUnZipBar.Value = 100;
                MainSpeedText.Text = "本地安装中...";
                break;

            case InstallStates.Cleared:
                InsInstallGameBar.IsIndeterminate = true;
                break;

            case InstallStates.Registering:
                InsInstallGameBar.IsIndeterminate = false;
                break;

            case InstallStates.Registered:
                InsInstallGameBar.Value = 100;
                break;
        }
    }

    private void ShowErrorDialog(string title, string message, Exception ex)
    {
        if (ex != null) message += $"\n\n错误详情：{ex.Message}";

        DialogHost.Show(new DialogInfo
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            AccountButton = DialogButtons.CloseButton
        });
    }

    public static void Install(
        BuildInfo info, 
        string url,
        bool? isUsePack, 
        string dir,
        string gameName,
        Action? installedCallBack = null)
    {
        var body = new TaskDownloadGameItem(info, url, isUsePack, dir, gameName);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
            installedCallBack?.Invoke();
        });
    }
    
    // 辅助方法：格式化字节大小
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}