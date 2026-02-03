using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportIntegrationPackItem : UserControl
{
    public TaskImportIntegrationPackItem()
    {
        InitializeComponent();
    }
    
    public Action? SuccessCallBack { get; set; }
    
    public TaskImportIntegrationPackItem(
        string filePath,
        string installFolder,
        string installName):this()
    {
        MainProgressBar.IsIndeterminate = true;
        Task.Run(async () =>
        {
            var installer = new IntegrationInstaller(filePath);
            installer.IntegrationProgress = new Progress<InstallIntegrationProgress>((progress) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    MainText.Text = $"({progress.Progress:F2} %) {progress.Message}";
                });
                
                if (progress.Status == InstallIntegrationProgressType.GetUrl)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsGetUrlBar.IsIndeterminate = true;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.DownloadingFile)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsGetUrlBar.IsIndeterminate = false;
                        InsGetUrlBar.Value = 100;
                        InsDownGameBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.DownloadedFile)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsMergeBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.Installing)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.Installing)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.Installed ||
                    progress.Status == InstallIntegrationProgressType.Uninstalling)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = 100;
                        InsInstallGameBar.Value = (int)progress.Progress;
                    });
                }

                if (progress.Status == InstallIntegrationProgressType.Success)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsInstallGameBar.Value = (int)progress.Progress;
                    });
                    SuccessCallBack?.Invoke();
                }
            });
            installer.BeginInstaller(installFolder, installName);
        });
    }

    public static void Install(
        string filePath,
        string installFolder,
        string installName)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "导入整合包",
            Message = $"整合包安装已开启后台进程",
            NoticeType = NoticeType.Info
        });

        var body = new TaskImportIntegrationPackItem(filePath, installFolder, installName);
        var tuid = GlobalModel.TaskManager.AddTask(body);
        body.SuccessCallBack = () =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
        };
    }
}