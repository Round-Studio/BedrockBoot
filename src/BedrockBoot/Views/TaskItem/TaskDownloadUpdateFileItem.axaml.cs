using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;
using Path = System.IO.Path;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadUpdateFileItem : UserControl
{
    private readonly string _currentExecutablePath = GetCurrentLauncherPath();

    private Action _cancelCallBack = () => { };
    private CancellationTokenSource? _cts;

    public TaskDownloadUpdateFileItem()
    {
        InitializeComponent();
    }

    public TaskDownloadUpdateFileItem(Release release) : this()
    {
        Release = release;
    }

    public Release Release { get; set; } = null!;

    public void Update(Action cancelCallBack)
    {
        _cancelCallBack = cancelCallBack;
        CardTitle.Text = string.Format(I18nManager.Instance["Task.Update.Title.Format"], Release.TagName);

        var asset = SelectPreferredAsset();
        if (asset == null)
        {
            Console.WriteLine(@"未找到适用于当前平台的更新资源");
            return;
        }

        Directory.CreateDirectory(PathsList.UpdatePath);

        var downloadUrl = asset.BrowserDownloadUrl;
        var downloadPath = Path.Combine(PathsList.UpdatePath, asset.Name);

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            var download = new GithubFilesDownloader(
                GlobalModel.Config.Data.DownloadChunkCount,
                1024);

            await download.DownloadAsync(
                downloadUrl,
                downloadPath,
                new Progress<DownloadProgress>(progress =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ProgressBar.IsIndeterminate)
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Value = 100;
                        }

                        ProgressBar.Value = (int)progress.ProgressPercentage;
                        ProgressText.Text = $"{progress.ProgressPercentage:F2} %";
                    });
                }),
                token);

            await Task.Delay(100, token);

            if (string.IsNullOrWhiteSpace(_currentExecutablePath) || !File.Exists(_currentExecutablePath))
                throw new FileNotFoundException("无法定位当前程序，不能启动更新引导流程", _currentExecutablePath);

            AppUpdater.EnsureExecutableForCurrentPlatform(downloadPath);

            // 下载完成后，启动新版本时使用 -updatev2 参数
            var startInfo = new ProcessStartInfo
            {
                FileName = _currentExecutablePath,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-updatev2");
            startInfo.ArgumentList.Add(downloadPath);  // 参数是新文件路径

            Process.Start(startInfo);

            await Task.Delay(100, token);
            Environment.Exit(0);
        }, token);
    }

    public static void Update(Release release)
    {
        Models.Global.GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.Update.Notice.Title"],
            Message = I18nManager.Instance["Task.Update.Notice.Message"],
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadUpdateFileItem(release);
        var taskId = Models.Global.GlobalModel.TaskManager.AddTask(body);

        body.Update(() => Models.Global.GlobalModel.TaskManager.RemoveTask(taskId));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _cancelCallBack.Invoke();
    }

    /// <summary>
    ///     根据当前平台选择可直接替换的发行资源。
    ///     Windows 选择单文件 .exe，Linux 选择可直接启动的 .AppImage。
    /// </summary>
    private ReleaseAsset? SelectPreferredAsset()
    {
        var assets = Release.Assets.ToList();
        if (assets.Count == 0)
            return null;

        if (OperatingSystem.IsWindows())
            return assets.FirstOrDefault(asset =>
                       asset.Name.Contains("win", StringComparison.OrdinalIgnoreCase) &&
                       asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                   ?? assets.FirstOrDefault(asset =>
                       asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        if (OperatingSystem.IsLinux())
            return assets.FirstOrDefault(asset =>
                       asset.Name.Contains("linux", StringComparison.OrdinalIgnoreCase) &&
                       asset.Name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                   ?? assets.FirstOrDefault(asset =>
                       asset.Name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase));

        return null;
    }

    private static string GetCurrentLauncherPath()
    {
        if (OperatingSystem.IsLinux())
        {
            var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (!string.IsNullOrWhiteSpace(appImagePath))
                return appImagePath;
        }

        return Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    }
}
