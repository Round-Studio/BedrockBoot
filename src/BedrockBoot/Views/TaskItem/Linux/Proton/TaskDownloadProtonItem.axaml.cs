using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Global;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem.Linux.Proton;

public partial class TaskDownloadProtonItem : UserControl
{
    private readonly ProtonInfo _info;
    private readonly InstallInfo _installInfo;
    
    public Action? CallBack { get; set; }

    public TaskDownloadProtonItem()
    {
        InitializeComponent();
    }

    public TaskDownloadProtonItem(ProtonInfo info, InstallInfo installInfo) : this()
    {
        _info = info;
        _installInfo = installInfo;
    }

    public async Task Install()
    {
        Task.Run(async () =>
        {
            await ProtonCore.InstallProton(_info, _installInfo, new Progress<DownloadProgress>(p =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressText.Text = $"{p.Message} ({p.ProgressPercentage:F2} %)";
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = (int)p.ProgressPercentage;
                });
            }));

            CallBack?.Invoke();
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

        body.CallBack = () => GlobalModel.TaskManager.RemoveTask(tuid);
        _ = body.Install();
    }
}