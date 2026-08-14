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
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Downloader.Files;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadCurseForgeResourceItem : UserControl, ITaskItem
{
    public TaskDownloadCurseForgeResourceItem()
    {
        InitializeComponent();
    }

    public TaskDownloadCurseForgeResourceItem(CurseForgeResponse.ModFile modFile) : this()
    {
        ModFile = modFile;
        _taskTitle = string.Format(I18nManager.Instance["Task.CurseForge.Title.Format"], ModFile.DisplayName);
        Update();
    }

    public CurseForgeResponse.ModFile ModFile { get; set; }
    public Action CallBack { get; set; }
    private CancellationTokenSource _cts;

    private double _taskProgress;
    private string _taskStatusText = "";
    private string _taskTitle = "";
    private bool _taskIsCompleted;
    private bool _taskIsIndeterminate = true;

    public double Progress => _taskProgress;
    public string StatusText => _taskStatusText;
    public string Title => _taskTitle;
    public bool IsCompleted => _taskIsCompleted;
    public bool IsIndeterminate => _taskIsIndeterminate;

    public event Action<ITaskItem>? ProgressUpdated;

    protected void ReportProgress(double progress, string statusText, bool isIndeterminate = false)
    {
        _taskProgress = progress;
        _taskStatusText = statusText;
        _taskIsIndeterminate = isIndeterminate;
        if (progress >= 100) _taskIsCompleted = true;
        ProgressUpdated?.Invoke(this);
    }

    public void Update()
    {
        CardTitle.Text = _taskTitle;
    }

    public async Task Download(string savePath, VersionConfig? version = null)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var download = new MultiThreadDownloader
        {
            AdditionalHeaders = new Dictionary<string, string>
            {
                { "x-api-key", GlobalKeys.CurseForgeApiKey }
            }
        };

        var url = new Uri(ModFile.DownloadUrl).AbsoluteUri.Replace("edge.forgecdn.net", "mediafilez.forgecdn.net");
        Console.WriteLine($@"下载文件：{url}");

        await download.DownloadAsync(url, savePath, new Progress<DownloadProgress>(xprogress =>
        {
            ReportProgress(xprogress.ProgressPercentage, string.Format(I18nManager.Instance["Task.CurseForge.Status.Progress"], xprogress.ProgressPercentage));
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DownloadProgressBar.IsIndeterminate) DownloadProgressBar.IsIndeterminate = false;

                DownloadProgressBar.Value = xprogress.ProgressPercentage;
                MainText.Text = string.Format(I18nManager.Instance["Task.CurseForge.Status.Progress"],
                    xprogress.ProgressPercentage);
                MainSpeedText.Text = "??? / s";
            });
        }), token);

        if (version != null)
        {
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

        Dispatcher.UIThread.Invoke(CallBack);
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