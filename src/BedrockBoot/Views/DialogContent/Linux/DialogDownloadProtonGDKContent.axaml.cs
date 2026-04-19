using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
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
        var downloader = new ProtonDownloader();
        await downloader.Download(new Progress<DownloadProgress>(p =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = (int)p.ProgressPercentage;
                ProgressText.Text = $"下载 ProtonGDK ({p.ProgressPercentage:F2} %)";
            });
        }));

        DialogHost.Close();
    }
}