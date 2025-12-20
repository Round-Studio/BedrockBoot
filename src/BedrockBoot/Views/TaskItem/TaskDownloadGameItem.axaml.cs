using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;
using Downloader;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadGameItem : UserControl
{
    public string InstallFolder { get; set; }
    public string GameName { get; set; }
    public BuildInfo BuildInfo { get; set; }
    public TaskDownloadGameItem()
    {
        InitializeComponent();
    }

    public TaskDownloadGameItem(BuildInfo info, string dir, string gameName) : this()
    {
        BuildInfo = info;
        InstallFolder = dir;
        GameName = gameName;
    }
    static string FormatBytes(double bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        double number = bytes;

        while (number >= 1024 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:F1} {suffixes[counter]}";
    }
    public void Install(Action installed)
    {
        CardTitle.Text = $"下载游戏 {BuildInfo.ID}";

        InsGetUrlBar.IsIndeterminate = true;
        if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            InsInstallGamePanel.IsVisible = false;
        
        Task.Run(async () =>
        {
            if (!Directory.Exists(Path.Combine(InstallFolder, "version_save")))
                Directory.CreateDirectory(Path.Combine(InstallFolder, "version_save"));
            var path = Path.Combine(InstallFolder, "version_save", $"{BuildInfo.ID}.insPack");

            if (!File.Exists(path))
                try
                {
                    var url = GlobalModel.BedrockCore.GetPackageUri(BuildInfo, Architecture.X64).Result;
                    var downloadService = new DownloadService(GlobalModel.DownloadConfiguration);
                    downloadService.DownloadStarted += (sender, args) =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (InsGetUrlBar.IsIndeterminate)
                            {
                                InsGetUrlBar.IsIndeterminate = false;
                                InsGetUrlBar.Value = 100;
                            }

                            InsDownGameBar.Value = 0;
                            MainText.Text = $"步骤：开始下载";
                            MainSpeedText.Text = $"0 B / s";
                        });
                    };
                    downloadService.DownloadProgressChanged += (sender, args) =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (InsGetUrlBar.IsIndeterminate)
                            {
                                InsGetUrlBar.IsIndeterminate = false;
                                InsGetUrlBar.Value = 100;
                            }

                            InsDownGameBar.Value = args.ProgressPercentage;
                            MainText.Text = $"步骤：下载游戏 ({args.ProgressPercentage:F2}%)";
                            MainSpeedText.Text = $"{FormatBytes(args.BytesPerSecondSpeed)} / s";
                        });
                    };
                    downloadService.DownloadFileCompleted += (sender, args) =>
                    {
                        Dispatcher.UIThread.Invoke(() => InsMergeBar.IsIndeterminate = false);
                        Dispatcher.UIThread.Invoke(() => InsMergeBar.Value = 100);
                    };
                    await downloadService.DownloadFileTaskAsync(url, path);
                }
                catch(Exception ex)
                {
                    Dispatcher.UIThread.Invoke(() => 
                        installed?.Invoke());

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Show(new DialogInfo()
                        {
                            Title = $"下载错误：{BuildInfo.ID}",
                            Content = $"抱歉，我们发生了一些错误。\n" +
                                      $"这可能是微软已经把该版本删除，也有可能是您的网络问题。\n\n" +
                                      $"{ex.Message}",
                            CloseButtonText = "确定",
                            AccountButton = DialogButtons.CloseButton
                        });
                    });

                    if (Directory.Exists(Path.Combine(InstallFolder, "bedrock_versions", GameName)))
                    {
                        Directory.Delete(Path.Combine(InstallFolder, "bedrock_versions", GameName), true);
                    }
                    
                    return;
                }

            await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
            {
                FileFullPath = path,
                GameName = GameName,
                InstallDstFolder = Path.Combine(InstallFolder, "bedrock_versions", GameName),
                GameTypeVersion = BuildInfo.Type,
                Type = BuildInfo.BuildType,
                ExtractionProgress = new Progress<DecompressProgress>(ext =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsUnZipBar.Value = ext.Percentage;
                        MainText.Text = $"步骤：解压文件 ({ext.Percentage:F2}%)";
                    });
                }),
                DeployProgress = new Progress<DeploymentProgress>(((s) =>
                {
                    Console.WriteLine($"{s.state} - {s.percentage}");

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        InsInstallGameBar.Value = s.percentage;
                        MainText.Text = $"步骤：部署游戏 ({s} [{s.percentage}%])";
                    });
                })),
                InstallStates = new Progress<InstallStates>((states) =>
                {
                    Console.WriteLine(states);

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (states == InstallStates.Extracting)
                        {
                            InsUnZipBar.IsIndeterminate = false;
                        }
                        else if (states == InstallStates.Extracted)
                        {
                            InsUnZipBar.Value = 100;

                            if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
                            {
                                GameInfoHelper.SaveVersionConfig(new VersionConfig()
                                {
                                    VersionPath = Path.Combine(InstallFolder, "bedrock_versions", GameName),
                                    Info = new VersionConfig.VersionInfo()
                                    {
                                        BuildType = BuildInfo.BuildType,
                                        Version = BuildInfo.ID,
                                        VersionName = GameName,
                                        VersionType = BuildInfo.Type
                                    }
                                });

                                Dispatcher.UIThread.Invoke(() => 
                                    installed?.Invoke());
                            }
                        }
                        else if (states == InstallStates.Cleared)
                        {
                            InsInstallGameBar.IsIndeterminate = true;
                        }
                        else if (states == InstallStates.Registering)
                        {
                            InsInstallGameBar.IsIndeterminate = false;
                        }
                        else if (states == InstallStates.Registered)
                        {
                            InsInstallGameBar.Value = 100;
                            
                            GameInfoHelper.SaveVersionConfig(new VersionConfig()
                            {
                                VersionPath = Path.Combine(InstallFolder, "bedrock_versions", GameName),
                                Info = new VersionConfig.VersionInfo()
                                {
                                    BuildType = BuildInfo.BuildType,
                                    Version = BuildInfo.ID,
                                    VersionName = GameName,
                                    VersionType = BuildInfo.Type
                                }
                            });

                            Dispatcher.UIThread.Invoke(() => 
                                installed?.Invoke());
                        }
                    });
                })
            });
        });
    }

    public static void Install(BuildInfo info, string dir, string gameName)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
        {
            Title = "下载游戏",
            Message = $"游戏 {gameName} 已将其下载任务添加至任务列表。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadGameItem(info, dir, gameName);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }
}