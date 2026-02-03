using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Integration;

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
        Task.Run(() =>
        {
            var installer = new IntegrationInstaller(filePath);
            installer.IntegrationProgress = new Progress<InstallIntegrationProgress>((progress) =>
            {
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
}