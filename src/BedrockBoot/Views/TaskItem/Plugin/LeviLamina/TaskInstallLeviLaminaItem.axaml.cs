using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Base.Enum;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem.Plugin.LeviLamina;

public partial class TaskInstallLeviLaminaItem : UserControl
{
    public TaskInstallLeviLaminaItem()
    {
        InitializeComponent();
    }
    
    public VersionConfig VersionConfig { get; set; }
    public string LeviLaminaVersion { get; set; }
    public Action? CompleteCallBack { get; set; }

    public TaskInstallLeviLaminaItem(string version, VersionConfig versionConfig) : this()
    {
        VersionConfig = versionConfig;
        LeviLaminaVersion = version;
    }

    public void Install()
    {
        Task.Run(() =>
        {
            var installer = new LeviLaminaInstaller(VersionConfig);
            installer.Progress = new Progress<InstallerProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    MainText.Text = p.Message;
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
                    }
                });
            });
            installer.InstallLeviLamina(LeviLaminaVersion);
        });
    }

    public static void Install(string version, VersionConfig versionConfig)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "安装 LeviLamina",
            Message = $"已将其安装任务添加至任务列表。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskInstallLeviLaminaItem(version, versionConfig);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CompleteCallBack = () => GlobalModel.TaskManager.RemoveTask(tuid);
        body.Install();
    }
}