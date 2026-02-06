using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Models.Download;
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
        CardTitle.Text = $"下载更新文件：{Release.TagName}";
        var url = Release.Assets[0].BrowserDownloadUrl;

        SourceList.UpdateDownloadSources.ToList().ForEach(src =>
        {
            var thisUrl = src.Value.Replace("{url}", url);
            var path = Path.Combine(PathsList.UpdatePath, $"{src.Key}_{Release.TagName}.exe");
            var progress = new ProgressBar
            {
                IsIndeterminate = true
            };
            var item = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new TextBlock
                    {
                        MinWidth = 120,
                        Text = src.Key
                    },
                    progress
                }
            };
            
            Task.Run(async () =>
            {
                var download = new MultiThreadDownloader(GlobalModel.Config.Data.DownloadChunkCount, 1024);

                await download.DownloadAsync(thisUrl, path, new Progress<DownloadProgress>(xprogress =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (progress.IsIndeterminate)
                        {
                            progress.IsIndeterminate = false;
                            progress.Value = 100;
                        }

                        progress.Value = xprogress.ProgressPercentage;
                    });
                }));

                Thread.Sleep(100);

                Process.Start(path, new[] { "-update", Process.GetCurrentProcess().MainModule?.FileName });
                Thread.Sleep(100);

                Environment.Exit(0);
            });

            SourceListBox.Children.Add(item);
        });
    }

    public static void Update(Release release)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "下载更新",
            Message = "正在下载更新文件",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadUpdateFileItem(release);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Update();
    }
}