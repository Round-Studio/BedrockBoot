using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadGameItem : UserControl, ITaskItem
{
    private EasyDownload _downloader;
    private CancellationTokenSource _cancellationTokenSource;
    private string _taskStage = "";
    private double _taskProgress;
    private string _taskStatusText = "";
    private string _taskTitle = "";
    private bool _taskIsCompleted;
    private bool _taskIsIndeterminate = true;

    public double Progress => _taskProgress;
    public string StatusText => _taskStatusText;
    public string Title => _taskTitle;
    public bool IsCompleted => _taskIsCompleted;
    public bool IsIndeterminate => _taskIsIndeterminate;

    public event Action<ITaskItem>? ProgressUpdated;

    protected void ReportProgress(double progress, string statusText, bool isIndeterminate = false)
    {
        _taskProgress = progress;
        _taskStatusText = statusText;
        _taskIsIndeterminate = isIndeterminate;
        if (progress >= 100) _taskIsCompleted = true;
        ProgressUpdated?.Invoke(this);
    }

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

        _taskTitle = string.Format(I18nManager.Instance["Task.Game.Title.Format"], GameName, info.ID);
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
        ReportProgress(35 + percentage * 0.15, text);
    }

    public void Install(Action installed)
    {
        CardTitle.Text = _taskTitle;

        InsGetUrlBar.IsIndeterminate = true;
        if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            InsInstallGamePanel.IsVisible = false;

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                await _downloader.InstallAsync(Url, token);
                installed?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
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
        ReportProgress(progressInfo.Percentage * 0.35, text);

        if (!string.IsNullOrEmpty(progressInfo.Speed))
            MainSpeedText.Text = string.Format(I18nManager.Instance["Common.Unit.Speed"], progressInfo.Speed);
    }

    private void UpdateExtractionProgress(string text, double percentage)
    {
        InsUnZipBar.IsIndeterminate = false;
        InsUnZipBar.Value = percentage;
        MainText.Text = text;
        ReportProgress(50 + percentage * 0.25, text);
    }

    private void UpdateDeploymentProgress(string text, DeploymentProgress progress)
    {
        InsInstallGameBar.Value = progress.percentage;
        MainText.Text = text;
        ReportProgress(75 + progress.percentage * 0.25, text);
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
                ReportProgress(75, I18nManager.Instance["Task.Game.Status.LocalInstalling"]);
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
                ReportProgress(100, I18nManager.Instance["Task.Game.Status.Completed"]);
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

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }
}