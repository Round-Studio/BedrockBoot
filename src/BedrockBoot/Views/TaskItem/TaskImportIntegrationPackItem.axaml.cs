using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportIntegrationPackItem : UserControl
{
    private CancellationTokenSource? _cts;

    public TaskImportIntegrationPackItem()
    {
        InitializeComponent();
    }

    public TaskImportIntegrationPackItem(
        string filePath,
        string installFolder,
        string installName) : this()
    {
        MainProgressBar.IsIndeterminate = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Task.Run(async () =>
        {
            var installer = new IntegrationInstaller(filePath);
            installer.IntegrationProgress = new Progress<InstallIntegrationProgress>(progress =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // 进度文字格式化国际化
                    MainText.Text = string.Format(I18nManager.Instance["Task.IntegrationPack.Status.Format"],
                        progress.Progress, progress.Message);
                });

                if (progress.Status == InstallIntegrationProgressType.GetUrl)
                    Dispatcher.UIThread.Invoke(() => { InsGetUrlBar.IsIndeterminate = true; });

                if (progress.Status == InstallIntegrationProgressType.DownloadingFile)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsGetUrlBar.IsIndeterminate = false;
                        InsGetUrlBar.Value = 100;
                        InsDownGameBar.Value = (int)progress.Progress;
                    });

                if (progress.Status == InstallIntegrationProgressType.DownloadedFile)
                    Dispatcher.UIThread.Invoke(() => { InsMergeBar.Value = (int)progress.Progress; });

                if (progress.Status == InstallIntegrationProgressType.Installing)
                    Dispatcher.UIThread.Invoke(() => { InsUnZipBar.Value = (int)progress.Progress; });

                if (progress.Status == InstallIntegrationProgressType.Installed ||
                    progress.Status == InstallIntegrationProgressType.Uninstalling)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = 100;
                        InsInstallGameBar.Value = (int)progress.Progress;
                    });

                if (progress.Status == InstallIntegrationProgressType.Success)
                {
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