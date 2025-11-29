using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Native;
using BedrockLauncher.Core.Network;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Entity;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadGameItem : UserControl
{
    public string InstallFolder { get; set; }
    public string GameName { get; set; }
    public VersionInformation VersionInformation { get; set; }
    public List<GameFileInfo> FileList { get; set; } = new();
    public TaskDownloadGameItem()
    {
        InitializeComponent();
    }

    public TaskDownloadGameItem(VersionInformation info, string dir, string gameName) : this()
    {
        VersionInformation = info;
        InstallFolder = dir;
        GameName = gameName;
    }

    public void Install(Action installed)
    {
        CardTitle.Text = $"下载游戏 {VersionInformation.ID}";
        GameInfoHelper.SaveVersionConfig(new VersionConfig()
        {
            VersionPath = Path.Combine(InstallFolder, "bedrock_versions", GameName),
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = GameBuildType.Uwp,
                Version = VersionInformation.ID,
                VersionName = GameName,
                VersionType = GameInfoHelper.GetGameVersionType(VersionInformation.Type)
            }
        });
        
        var cls = new DownloadSpeedCalculator();
        InstallCallback callback = new InstallCallback()
        {
            zipProgress = new Progress<ZipProgress>((progress =>
            {
                Console.WriteLine(progress.ToString());
                FileList.Add(new GameFileInfo()
                {
                    FilePath = progress.CurrentFileName
                });
                Dispatcher.UIThread.Invoke(() =>
                {
                    InsUnZipBar.Value = progress.Percentage;
                    MainText.Text = $"步骤：解压文件 ({progress.Percentage:F2}%)";
                });
            })),
            downloadProgress = (new Progress<DownloadProgress>((p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    InsDownGameBar.Value = p.ProgressPercentage;
                    MainText.Text = $"步骤：下载文件 ({p.ProgressPercentage:F2}%)";
                    MainSpeedText.Text = $"{(int)cls.UpdateSpeed(p.DownloadedBytes, p.TotalBytes)} Kb/s";
                });

                if (p.TotalBytes > 0)
                {
                    Console.WriteLine(
                        $"下载进度: {p.ProgressPercentage:F2}% ({p.DownloadedBytes / (1024.0 * 1024):F2} MB / {p.TotalBytes / (1024.0 * 1024):F2} MB)");
                }
                else
                {
                    Console.WriteLine($"已下载: {p.DownloadedBytes / (1024.0 * 1024):F2} MB (总大小未知)");
                }
            }))),
            registerProcess_percent = ((s, u) =>
            {
                Console.WriteLine(s + u);

                Dispatcher.UIThread.Invoke(() =>
                {
                    InsInstallGameBar.Value = u;
                    MainText.Text = $"步骤：部署游戏 ({s} [{u}%])";
                });
            }),
            result_callback = ((status, exception) =>
            {
                if (status == AsyncStatus.Completed)
                {
                    Dispatcher.UIThread.Invoke(() => BuildIndex(installed));
                }
            }),
            install_states = (states =>
            {
                Console.WriteLine(states);

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (states == InstallStates.getingDownloadUri)
                        InsGetUrlBar.IsIndeterminate = true;
                    else if (states == InstallStates.gotDownloadUri)
                    {
                        InsGetUrlBar.Value = 100;
                        InsGetUrlBar.IsIndeterminate = false;
                        InsDownGameBar.IsIndeterminate = true;
                    }
                    else if (states == InstallStates.downloading)
                    {
                        InsDownGameBar.IsIndeterminate = false;
                    }
                    else if (states == InstallStates.downloaded)
                    {
                        InsUnZipBar.IsIndeterminate = true;
                        MainSpeedText.Text = $"0 B/s";
                    }
                    else if (states == InstallStates.unzipng)
                    {
                        InsUnZipBar.IsIndeterminate = false;
                    }
                    else if (states == InstallStates.registered)
                    {
                        InsInstallGameBar.IsIndeterminate = true;
                    }
                    else if (states == InstallStates.registering)
                    {
                        InsInstallGameBar.IsIndeterminate = false;
                    }
                });
            })
        };

        Task.Run(() =>
        {
            GlobalModel.BedrockCore.InstallVersion(VersionInformation.Variations[0],
                GameInfoHelper.GetGameVersionType(VersionInformation.Type), $"./{VersionInformation.ID}.appx", GameName,
                Path.Combine(InstallFolder, "bedrock_versions", GameName), callback);
        });
    }

    public void BuildIndex(Action installed)
    {
        InsBuildIndex.Maximum = FileList.Count;
        MainText.Text = $"步骤：构建引索 (0%)";
        Task.Run(() =>
        {
            FileList.ForEach(file =>
            {
                file.Hash = FileHashCalculator.CalculateHash(Path.Combine(InstallFolder, GameName, file.FilePath),
                    FileHashCalculator.HashType.MD5);
            });

            var entry = new ConfigEntity<List<GameFileInfo>>(Path.Combine(InstallFolder, GameName, "config",
                "BedrockBoot2", "index.json"));

            entry.Data = FileList;
            entry.Save();
            
            installed?.Invoke();
        });
    }

    public static void Install(VersionInformation info, string dir, string gameName)
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