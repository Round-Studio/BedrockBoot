using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Download;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Helper;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadCurseForgeResourceItem : UserControl
{
    public CurseForgeResponse.ModFile ModFile { get; set; }
    public Action CallBack { get; set; }
    public TaskDownloadCurseForgeResourceItem()
    {
        InitializeComponent();
    }

    public TaskDownloadCurseForgeResourceItem(CurseForgeResponse.ModFile modFile) : this()
    {
        ModFile = modFile;
        Update();
    }

    public void Update()
    {
        CardTitle.Text = $"下载资源：{ModFile.DisplayName}";
    }

    public async Task Download(string savePath)
    {
        var download = new SingleThreadDownloader(1, 1024);

        var url = SourceList.CurseForgeSource.ToList()[GlobalModel.Config.Data.CurseForgeSourceIndex].Value
            .Replace("{url}", ModFile.DownloadUrl);
        Console.WriteLine($"下载文件：{url}");
        await download.DownloadAsync(url, savePath, new Progress<SingleThreadDownloader.DownloadProgress>((xprogress =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DownloadProgressBar.IsIndeterminate)
                {
                    DownloadProgressBar.IsIndeterminate = false;
                }

                DownloadProgressBar.Value = xprogress.ProgressPercentage;
                MainText.Text = $"进度：{xprogress.ProgressPercentage:F2} %";
                MainSpeedText.Text = $"{SizeHelper.FormatBytes(xprogress.BytesPerSecond)} / s";
            });
        })));
        
        CallBack?.Invoke();
    }
    
    public static void Download(CurseForgeResponse.ModFile modFile, string savePath)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
        {
            Title = "下载资源",
            Message = $"资源 {modFile.DisplayName} 已将其下载任务添加至任务列表。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadCurseForgeResourceItem(modFile);
        var tuid = GlobalModel.TaskManager.AddTask(body);
        
        body.CallBack = ()=>GlobalModel.TaskManager.RemoveTask(tuid);
        body.Download(savePath);
    }
}