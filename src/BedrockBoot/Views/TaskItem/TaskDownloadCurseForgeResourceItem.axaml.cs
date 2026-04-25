using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
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
    private CancellationTokenSource _cts;

    public void Update()
    {
        // 使用动态格式化字符串
        CardTitle.Text = string.Format(I18nManager.Instance["Task.CurseForge.Title.Format"], ModFile.DisplayName);
    }

    public async Task Download(string savePath, VersionConfig version = null)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var download = new MultiThreadDownloader();

        var url = new Uri(ModFile.DownloadUrl).AbsoluteUri.Replace("edge.forgecdn.net", "mediafilez.forgecdn.net");
        Console.WriteLine($@"下载文件：{url}");

        await download.DownloadAsync(url, savePath, new Progress<DownloadProgress>(xprogress =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DownloadProgressBar.IsIndeterminate) DownloadProgressBar.IsIndeterminate = false;

                DownloadProgressBar.Value = xprogress.ProgressPercentage;
                // 进度文字国际化
                MainText.Text = string.Format(I18nManager.Instance["Task.CurseForge.Status.Progress"],
                    xprogress.ProgressPercentage);
                MainSpeedText.Text = "??? / s";
            });
        }), token);

        if (version == null) CallBack?.Invoke();

        DownloadProgressBar.IsIndeterminate = true;
        // 导入状态国际化
        MainText.Text = I18nManager.Instance["Task.CurseForge.Status.Importing"];

        Task.Run(() =>
        {
            if (savePath.EndsWith(".mcworld"))
            {
                var worldManager = new ArchiveCheck(version);
                worldManager.ImportWorldPack(savePath);
            }
            else
            {
                var manager = new ResourcePackManager(version);
                manager.GetAllPack();
                manager.AddRangePacks(new List<string> { savePath });
            }

            if (CallBack != null) Dispatcher.UIThread.Invoke(CallBack);
        }, token);
    }

    public static void Download(CurseForgeResponse.ModFile modFile, string savePath, VersionConfig version = null)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.CurseForge.Notice.Title"],
            Message = string.Format(I18nManager.Instance["Task.CurseForge.Notice.Added"], modFile.DisplayName),
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadCurseForgeResourceItem(modFile);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CallBack = () => GlobalModel.TaskManager.RemoveTask(tuid);
        _ = body.Download(savePath, version); // 使用丢弃符号明确表示异步调用
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CallBack?.Invoke();
    }
}