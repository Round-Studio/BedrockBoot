using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using Downloader;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using Path = System.IO.Path;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadUpdateFileItem : UserControl
{
    public Release Release { get; set; }

    public TaskDownloadUpdateFileItem()
    {
        InitializeComponent();
    }

    public TaskDownloadUpdateFileItem(Release release) : this()
    {
        Release = release;
    }

    public void Update()
    {
        CardTitle.Text = $"下载更新文件：{Release.TagName}";
        var url = Release.Assets[0].BrowserDownloadUrl;
        var path = Path.Combine(PathsList.UpdatePath, $"{Release.TagName}.exe");

        var service = new DownloadService();
        // Provide `FileName` and `TotalBytesToReceive` at the start of each downloads
        service.DownloadStarted += (sender, args) =>
        {
            Dispatcher.UIThread.Invoke(() => UpdateProgressBar.IsIndeterminate = false);
        };
        service.DownloadProgressChanged += (sender, args) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateProgressBar.Value = (int)args.ProgressPercentage;
                UpdateProgressText.Text = $"步骤：下载文件 ({args.ProgressPercentage:F} %)";
            });
        };
        service.DownloadFileCompleted += (sender, args) =>
        {
            Process.Start(path, new[] { "-update", Process.GetCurrentProcess().MainModule?.FileName });
            Thread.Sleep(100);
            
            Environment.Exit(0);
        };
        
        service.DownloadFileTaskAsync(url, path);
    }

    public static void Update(Release release)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
        {
            Title = "下载更新",
            Message = $"正在下载更新文件",
            NoticeType = NoticeType.Info
        });
        
        var body = new TaskDownloadUpdateFileItem(release);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Update();
    }
}