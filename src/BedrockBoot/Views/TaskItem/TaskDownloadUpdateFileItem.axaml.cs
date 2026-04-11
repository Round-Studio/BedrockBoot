using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using Path = System.IO.Path;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadUpdateFileItem : UserControl
{


    public TaskDownloadUpdateFileItem()
    {
        InitializeComponent();
    }

    public TaskDownloadUpdateFileItem(Release release) : this()
    {
        Release = release;
    }

    public Release Release { get; set; }

    public void Update()
    {
        // 标题国际化
        CardTitle.Text = string.Format(I18nManager.Instance["Task.Update.Title.Format"], Release.TagName);

        // 根据操作系统选择正确的资产
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // 选择匹配的资产
        var asset = isLinux
            ? Release.Assets.FirstOrDefault(a => a.Name.Contains("linux", StringComparison.OrdinalIgnoreCase))
            : Release.Assets.FirstOrDefault(a => a.Name.Contains("win", StringComparison.OrdinalIgnoreCase)
                                                 || a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));

        if (asset == null)
        {
            var osName = isLinux ? "Linux" : "Windows";
            var errorMsg = $"未找到适用于 {osName} 的更新文件";
            Dispatcher.UIThread.Invoke(() => { ProgressText.Text = errorMsg; });
            Console.WriteLine(errorMsg);
            return;
        }

        var url = asset.BrowserDownloadUrl;
        // Linux 和 Windows 使用不同的扩展名
        var extension = isLinux ? "" : ".exe"; // Linux 文件通常没有扩展名或者是 .AppImage
        var fileName = isLinux ? Release.TagName : $"{Release.TagName}.exe";
        var path = Path.Combine(PathsList.UpdatePath, fileName);

        Task.Run(async () =>
        {
            var download = new GithubFilesDownloader(BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount,
                1024);

            await download.DownloadAsync(url, path, new Progress<DownloadProgress>(xprogress =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (ProgressBar.IsIndeterminate)
                    {
                        ProgressBar.IsIndeterminate = false;
                        ProgressBar.Value = 100;
                    }

                    ProgressBar.Value = (int)xprogress.ProgressPercentage;
                    ProgressText.Text = $"{xprogress.ProgressPercentage:F2} %";
                });
            }));

            // 给予 UI 刷新的缓冲时间
            await Task.Delay(100);

            // 根据平台启动更新程序
            if (isLinux)
            {
                // Linux: 设置可执行权限并启动
                var chmodProcess = Process.Start("chmod", $"+x \"{path}\"");
                chmodProcess?.WaitForExit();

                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = $"-update {Process.GetCurrentProcess().MainModule?.FileName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
            }
            else
            {
                // Windows: 直接启动
                Process.Start(path, new[] { "-update", Process.GetCurrentProcess().MainModule?.FileName });
            }

            await Task.Delay(100);
            Environment.Exit(0);
        });
    }

    public static void Update(Release release)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.Update.Notice.Title"],
            Message = I18nManager.Instance["Task.Update.Notice.Message"],
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadUpdateFileItem(release);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Update();
    }
}