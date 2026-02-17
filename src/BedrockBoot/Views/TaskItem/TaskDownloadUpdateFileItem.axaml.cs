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

        SourceList.UpdateDownloadSources.ToList().ForEach(src =>
        {
            var thisUrl = src.Value.Replace("{url}", url);
            var path = Path.Combine(PathsList.UpdatePath, $"{src.Key}_{Release.TagName}.exe");
            
            var progress = new ProgressBar
            {
                IsIndeterminate = true
            };

            // 动态创建的项也需要处理文本
            var item = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new TextBlock
                    {
                        MinWidth = 120,
                        // 如果需要对下载源名称进行修饰，可以使用 Format
                        Text = string.Format(I18nManager.Instance["Task.Update.Source.Prefix"], src.Key)
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

                // 给予 UI 刷新的缓冲时间
                await Task.Delay(100);

                // 启动更新程序
                Process.Start(path, new[] { "-update", Process.GetCurrentProcess().MainModule?.FileName });
                
                await Task.Delay(100);
                Environment.Exit(0);
            });

            SourceListBox.Children.Add(item);
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