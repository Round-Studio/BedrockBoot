using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Models.Global;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem.Linux.Proton;

public partial class TaskDownloadProtonItem : UserControl, ITaskItem
{
    private readonly ProtonInfo _info;
    private readonly InstallInfo _installInfo;
    
    public Action? CallBack { get; set; }
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

    public TaskDownloadProtonItem()
    {
        InitializeComponent();
    }

    public TaskDownloadProtonItem(ProtonInfo info, InstallInfo installInfo) : this()
    {
        _info = info;
        _installInfo = installInfo;
        _taskTitle = "下载 Proton";
    }

    public void Install()
    {
        Task.Run(async () =>
        {
            await ProtonCore.InstallProton(_info, _installInfo, new Progress<DownloadProgress>(p =>
            {
                ReportProgress(p.ProgressPercentage, $"{p.Message} ({p.ProgressPercentage:F2} %)");
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressText.Text = $"{p.Message} ({p.ProgressPercentage:F2} %)";
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = (int)p.ProgressPercentage;
                });
            }));

            ReportProgress(100, "安装完成");
            Dispatcher.UIThread.Invoke(CallBack!);
        });
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {

    }

    public static void Install(ProtonInfo info, InstallInfo installInfo)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "下载 Proton",
            Message = "已添加下载任务至任务列表",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadProtonItem(info, installInfo);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CallBack = () =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
            GameProton.UpdateList?.Invoke();
        };
        body.Install();
    }
}