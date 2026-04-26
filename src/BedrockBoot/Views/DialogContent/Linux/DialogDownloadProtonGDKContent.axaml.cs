using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Proton.Enum;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Linux;

public partial class DialogDownloadProtonGDKContent : UserControl
{
    public DialogDownloadProtonGDKContent()
    {
        InitializeComponent();
    }

    public async Task Download()
    {
        ProtonCore.InitializeEnvironment();
        
        var lst = await ProtonCore.GetInstallableVersion(ProtonSource.LukasPAH);
        var info = lst?.ToList().FirstOrDefault();

        if (info != null)
        {
            Task.Run(async () =>
            {
                var path = await ProtonCore.InstallProton(info, new InstallInfo()
                {
                    InstallName = info.Version,
                    IsOverWrite = true
                }, new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ProgressBar.IsIndeterminate)
                            ProgressBar.IsIndeterminate = false;
                        ProgressBar.Value = (int)p.ProgressPercentage;
                        ProgressText.Text = $"下载 ProtonGDK ({p.ProgressPercentage:F2} %)";
                    });
                }), true);
                await Dispatcher.UIThread.InvokeAsync(DialogHost.Close);

                ProtonCore.Config.Data.SelectProtonPath = path;
                ProtonCore.Config.Save();
            });
        }
        else
        {
            DialogHost.Show(new DialogInfo()
            {
                Title = "下载失败",
                Content = "请检查网络是否有问题，然后重启启动器重试。",
                CloseButtonText = "确定",
                CloseAction = () => { Environment.Exit(0); }
            });
        }
    }
}