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
        
        var url = Release.Assets[0].BrowserDownloadUrl;
        var path = Path.Combine(PathsList.UpdatePath, $"{Release.TagName}.exe");

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

            // 启动更新程序
            Process.Start(path, new[] { "-update", Process.GetCurrentProcess().MainModule?.FileName });

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