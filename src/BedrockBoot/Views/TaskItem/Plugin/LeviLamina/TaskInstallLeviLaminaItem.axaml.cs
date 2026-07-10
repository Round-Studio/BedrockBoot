using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Base.Enum;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem.Plugin.LeviLamina;

public partial class TaskInstallLeviLaminaItem : UserControl, ITaskItem
{
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

    public TaskInstallLeviLaminaItem()
    {
        InitializeComponent();
    }

    public TaskInstallLeviLaminaItem(string version, VersionConfig versionConfig) : this()
    {
        VersionConfig = versionConfig;
        LeviLaminaVersion = version;
        _taskTitle = string.Format(I18nManager.Instance["Task.LeviLamina.Notice.Title"]);
    }

    public VersionConfig VersionConfig { get; set; }
    public string LeviLaminaVersion { get; set; }
    public Action? CompleteCallBack { get; set; }
    public Action<string>? ErrorCallBack { get; set; }

    public void Install()
    {
        Task.Run(() =>
        {
            var installer = new LeviLaminaInstaller(VersionConfig);
            installer.Progress = new Progress<InstallerProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // 如果 Installer 内部消息已经是中文或你希望覆盖它：
                    MainText.Text = GetStatusMessage(p.Status, p.Message);

                    switch (p.Status)
                    {
                        case InstallerStatus.DownloadSource:
                            InsDownSourceBar.Value = (int)p.Progress;
                            ReportProgress(p.Progress * 0.2, p.Message);
                            break;
                        case InstallerStatus.DownloadLeviLamina:
                            InsLLMBar.Value = (int)p.Progress;
                            ReportProgress(20 + p.Progress * 0.2, p.Message);
                            break;
                        case InstallerStatus.DownloadCrashLogger:
                            InsLogBar.Value = (int)p.Progress;
                            ReportProgress(40 + p.Progress * 0.2, p.Message);
                            break;
                        case InstallerStatus.DownloadBedrockRtd:
                            InsRuntimeBar.Value = (int)p.Progress;
                            ReportProgress(60 + p.Progress * 0.2, p.Message);
                            break;
                        case InstallerStatus.DownloadPreLoader:
                            InsPreLoaderBar.Value = (int)p.Progress;
                            ReportProgress(80 + p.Progress * 0.2, p.Message);
                            break;
                        case InstallerStatus.Complete:
                            ReportProgress(100, p.Message);
                            CompleteCallBack?.Invoke();
                            break;
                        case InstallerStatus.Error:
                            ErrorCallBack?.Invoke(p.Message);
                            break;
                    }
                });
            });
            installer.InstallLeviLamina(LeviLaminaVersion);
        });
    }

    private string GetStatusMessage(InstallerStatus status, string defaultMsg)
    {
        return status switch
        {
            InstallerStatus.DownloadSource => I18nManager.Instance["Task.LeviLamina.Status.DownloadSource"],
            InstallerStatus.DownloadLeviLamina => I18nManager.Instance["Task.LeviLamina.Status.DownloadLLM"],
            InstallerStatus.DownloadCrashLogger => I18nManager.Instance["Task.LeviLamina.Status.DownloadLogger"],
            InstallerStatus.DownloadBedrockRtd => I18nManager.Instance["Task.LeviLamina.Status.DownloadRuntime"],
            InstallerStatus.DownloadPreLoader => I18nManager.Instance["Task.LeviLamina.Status.DownloadLoader"],
            _ => defaultMsg
        };
    }

    public static void Install(string version, VersionConfig versionConfig)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.LeviLamina.Notice.Title"],
            Message = I18nManager.Instance["Task.LeviLamina.Notice.Added"],
            NoticeType = NoticeType.Info
        });

        var body = new TaskInstallLeviLaminaItem(version, versionConfig);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CompleteCallBack = () =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["Task.LeviLamina.Notice.Title"],
                Message = I18nManager.Instance["Task.LeviLamina.Notice.Success"],
                NoticeType = NoticeType.Info
            });
        };
        body.ErrorCallBack = ex =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["Task.LeviLamina.Notice.Title"],
                Message = I18nManager.Instance["Task.LeviLamina.Notice.Failed"],
                NoticeType = NoticeType.Info
            });

            DialogHost.Show(new DialogInfo
            {
                Title = I18nManager.Instance["Task.LeviLamina.Notice.Failed"],
                Content = ex,
                CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"]
            });
        };
        body.Install();
    }
}