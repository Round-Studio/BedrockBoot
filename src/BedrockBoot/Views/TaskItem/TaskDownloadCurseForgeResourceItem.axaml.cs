using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadCurseForgeResourceItem : UserControl
{
    public TaskDownloadCurseForgeResourceItem()
    {
        InitializeComponent();
    }

    public TaskDownloadCurseForgeResourceItem(CurseForgeResponse.ModFile modFile) : this()
    {
        ModFile = modFile;
        Update();
    }

    public CurseForgeResponse.ModFile ModFile { get; set; }
    public Action CallBack { get; set; }

    public void Update()
    {
        CardTitle.Text = $"下载资源：{ModFile.DisplayName}";
    }

    public async Task Download(string savePath, VersionConfig version = null)
    {
        var download = new MultiThreadDownloader();

        var url = new Uri(ModFile.DownloadUrl).AbsoluteUri.Replace("edge.forgecdn.net", "mediafilez.forgecdn.net");
        Console.WriteLine($@"下载文件：{url}");
        await download.DownloadAsync(url, savePath, new Progress<DownloadProgress>(xprogress =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DownloadProgressBar.IsIndeterminate) DownloadProgressBar.IsIndeterminate = false;

                DownloadProgressBar.Value = xprogress.ProgressPercentage;
                MainText.Text = $"进度：{xprogress.ProgressPercentage:F2} %";
                MainSpeedText.Text = "??? / s";
            });
        }));

        if (version == null) CallBack?.Invoke();

        DownloadProgressBar.IsIndeterminate = true;
        MainText.Text = "进度：正在导入文件... (0 %)";
        Task.Run(() =>
        {
            var manager = new ResourcePackManager(version);
            manager.GetAllPack();
            manager.AddRangePacks(new List<string> { savePath });

            if (CallBack != null) Dispatcher.UIThread.Invoke(CallBack);
        });
    }

    public static void Download(CurseForgeResponse.ModFile modFile, string savePath, VersionConfig version = null)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "下载资源",
            Message = $"资源 {modFile.DisplayName} 已将其下载任务添加至任务列表。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadCurseForgeResourceItem(modFile);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CallBack = () => GlobalModel.TaskManager.RemoveTask(tuid);
        body.Download(savePath, version);
    }
}