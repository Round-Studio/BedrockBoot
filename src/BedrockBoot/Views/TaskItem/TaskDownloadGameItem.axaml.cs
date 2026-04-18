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
            DownloadProgress = (text, progressInfo) =>
                Dispatcher.UIThread.Invoke(() => UpdateDownloadProgress(text, progressInfo)),

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

            Completed = c =>
                Dispatcher.UIThread.Invoke(() =>
                    MainSpeedText.Text = I18nManager.Instance["Task.Game.Status.Completed"])
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
        // 标题国际化
        CardTitle.Text = string.Format(I18nManager.Instance["Task.Game.Title.Format"], GameName, BuildInfo.ID);

        InsGetUrlBar.IsIndeterminate = true;
        if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            InsInstallGamePanel.IsVisible = false;

        Task.Run(async () =>
        {
            await _downloader.InstallAsync(Url);
            installed?.Invoke();
        });
    }

    private void UpdateDownloadProgress(string text, DownloadProgressInfo progressInfo)
    {
        if (InsGetUrlBar.IsIndeterminate)
        {
            InsGetUrlBar.IsIndeterminate = false;
            InsGetUrlBar.Value = 100;
        }

        InsDownGameBar.Value = progressInfo.Percentage;
        MainText.Text = text;

        if (!string.IsNullOrEmpty(progressInfo.Speed))
            // 速度单位国际化
            MainSpeedText.Text = string.Format(I18nManager.Instance["Common.Unit.Speed"], progressInfo.Speed);
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
                MainSpeedText.Text = I18nManager.Instance["Task.Game.Status.LocalInstalling"];
                break;

#if WINDOWS
            case InstallStates.Cleared:
                InsInstallGameBar.IsIndeterminate = true;
                break;

            case InstallStates.Registering:
                InsInstallGameBar.IsIndeterminate = false;
                break;

            case InstallStates.Registered:
                InsInstallGameBar.Value = 100;
                break;
#endif
        }
    }

    private void ShowErrorDialog(string title, string message, Exception ex)
    {
        if (ex != null)
            message += $"\n\n{string.Format(I18nManager.Instance["Task.Game.Error.Detail"], ex.Message)}";

        DialogHost.Show(new DialogInfo
        {
            Title = title,
            Content = message,
            CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"],
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
}