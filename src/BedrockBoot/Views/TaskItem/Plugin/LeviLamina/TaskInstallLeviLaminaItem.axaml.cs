using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Base.Enum;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem.Plugin.LeviLamina;

public partial class TaskInstallLeviLaminaItem : UserControl
{
    public TaskInstallLeviLaminaItem()
    {
        InitializeComponent();
    }

    public TaskInstallLeviLaminaItem(string version, VersionConfig versionConfig) : this()
    {
        VersionConfig = versionConfig;
        LeviLaminaVersion = version;
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
                            break;
                        case InstallerStatus.DownloadLeviLamina:
                            InsLLMBar.Value = (int)p.Progress;
                            break;
                        case InstallerStatus.DownloadCrashLogger:
                            InsLogBar.Value = (int)p.Progress;
                            break;
                        case InstallerStatus.DownloadBedrockRtd:
                            InsRuntimeBar.Value = (int)p.Progress;
                            break;
                        case InstallerStatus.DownloadPreLoader:
                            InsPreLoaderBar.Value = (int)p.Progress;
                            break;
                        case InstallerStatus.Complete:
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