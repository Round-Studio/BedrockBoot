using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Chunker;

public partial class DialogDownloadChunkerContent : UserControl
{
    public DialogDownloadChunkerContent()
    {
        InitializeComponent();
    }
    
    public void Download(Action action)
    {
        Task.Run(async () =>
        {
           await BedrockBoot.Chunker.Chunker.DownloadChunker(DownloadType.Github,
                new Progress<DownloadProgressEventArgs>(pro =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProgressText.Text = $"下载 Chunker ({pro.Percentage:F2} %)";
                        ProgressBar.Value = pro.Percentage;
                    });
                }));
           
           Dispatcher.UIThread.Invoke(DialogHost.Close);
           action.Invoke();
        });
    }
}