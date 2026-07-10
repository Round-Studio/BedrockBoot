using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportIntegrationPackItem : UserControl, ITaskItem
{
    private CancellationTokenSource? _cts;
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

    public TaskImportIntegrationPackItem()
    {
        InitializeComponent();
    }

    public TaskImportIntegrationPackItem(
        string filePath,
        string installFolder,
        string installName) : this()
    {
        _taskTitle = string.Format(I18nManager.Instance["Task.IntegrationPack.Status.Format"], 0, I18nManager.Instance["Task.IntegrationPack.Notice.Added"]);
        MainProgressBar.IsIndeterminate = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Task.Run(async () =>
        {
            var installer = new IntegrationInstaller(filePath);
            installer.IntegrationProgress = new Progress<InstallIntegrationProgress>(progress =>
            {
                var text = string.Format(I18nManager.Instance["Task.IntegrationPack.Status.Format"],
                    progress.Progress, progress.Message);

                Dispatcher.UIThread.Invoke(() =>
                {
                    MainText.Text = text;
                });

                if (progress.Status == InstallIntegrationProgressType.GetUrl)
                {
                    ReportProgress(0, text);
                    Dispatcher.UIThread.Invoke(() => { InsGetUrlBar.IsIndeterminate = true; });
                }

                if (progress.Status == InstallIntegrationProgressType.DownloadingFile)
                {
                    ReportProgress(progress.Progress * 0.35, text);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsGetUrlBar.IsIndeterminate = false;
                        InsGetUrlBar.Value = 100;
                        InsDownGameBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.DownloadedFile)
                {
                    ReportProgress(35 + progress.Progress * 0.15, text);
                    Dispatcher.UIThread.Invoke(() => { InsMergeBar.Value = (int)progress.Progress; });
                }

                if (progress.Status == InstallIntegrationProgressType.Installing)
                {
                    ReportProgress(50 + progress.Progress * 0.25, text);
                    Dispatcher.UIThread.Invoke(() => { InsUnZipBar.Value = (int)progress.Progress; });
                }

                if (progress.Status == InstallIntegrationProgressType.Installed ||
                    progress.Status == InstallIntegrationProgressType.Uninstalling)
                {
                    ReportProgress(75 + progress.Progress * 0.25, text);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = 100;
                        InsInstallGameBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.Success)
                {
                    ReportProgress(100, text);
                    Dispatcher.UIThread.Invoke(() => { InsInstallGameBar.Value = (int)progress.Progress; });
                    SuccessCallBack?.Invoke();
                }
            });
            await installer.BeginInstaller(installFolder, installName, token);
        });
    }

    public Action? SuccessCallBack { get; set; }

    public static void Install(
        string filePath,
        string installFolder,
        string installName)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.IntegrationPack.Notice.Title"],
            Message = I18nManager.Instance["Task.IntegrationPack.Notice.Added"],
            NoticeType = NoticeType.Info
        });

        var body = new TaskImportIntegrationPackItem(filePath, installFolder, installName);
        var tuid = GlobalModel.TaskManager.AddTask(body);
        body.SuccessCallBack = () => { GlobalModel.TaskManager.RemoveTask(tuid); };
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        // Optionally, remove the task or update UI
        SuccessCallBack?.Invoke();
    }
}