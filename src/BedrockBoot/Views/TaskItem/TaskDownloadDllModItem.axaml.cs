using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.DllMods;
using BedrockBoot.Models.Pack.Search;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadDllModItem : UserControl, ITaskItem
{
    private readonly VersionConfig _instance;
    private readonly ModFile _modFile;

    public TaskDownloadDllModItem()
    {
        InitializeComponent();
    }

    public TaskDownloadDllModItem(VersionConfig instance, ModFile modFile) : this()
    {
        _instance = instance;
        _modFile = modFile;
        CardTitle.Text = $"下载 Mod: {modFile.FileName}";
    }

    public void Install(Action? onCompleted = null)
    {
        var installer = new DllModsInstaller(_instance, _modFile);
        installer.Progress = new Progress<BedrockBoot.Base.Entry.Progress.DownloadProgress>(progress =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = progress.ProgressPercentage;
                ProgressBar.IsIndeterminate = false;
                ProgressText.Text = $"{progress.ProgressPercentage:F2} %";
            });

            Progress = progress.ProgressPercentage;
            Title = $"下载 Mod: {_modFile.FileName}";
        });

        _ = Task.Run(async () =>
        {
            await installer.Install();
            Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = 100;
                ProgressText.Text = "下载完成";
                IsCompleted = true;
            });

            onCompleted?.Invoke();
        });
    }

    public static void Install(VersionConfig instance, ModFile modFile)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "已添加至任务列表",
            Message = "已将 Mod 下载任务添加至任务列表，您可以在任务列表中查看下载进度。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadDllModItem(instance, modFile);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }

    public double Progress { get; set; }
    public string StatusText { get; set; }
    public string Title { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsIndeterminate { get; set; }
    public event Action<ITaskItem>? ProgressUpdated;
}