using ABI.System;
using BedrockBoot.Models.Classes.Download;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadUpdateFileContent : Page
    {
        private string _url;
        public DownloadUpdateFileContent(string url)
        {
            _url = url;
            InitializeComponent();
        }
        static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n2} {suffixes[counter]}";
        }

        public async Task StartDownload()
        {
            var downloader = new ChunkDownloader(chunkCount: global_cfg.cfg.JsonCfg.DownThread);

            var progress = new Progress<Models.Classes.Download.DownloadProgress>(p =>
            {
                DispatcherQueue.EnqueueAsync(() =>
                {
                    Down_ProgressBar.Value = p.Percentage;
                    Down_ProgressTextBlock.Text = $"{p.Percentage:F2} %";
                    Down_ProgressSpeedTextBlock.Text = $"{FormatBytes((long)(p.SpeedKbps * 1024))}/s";
                    Down_ProgressFileTextBlock.Text = $"{FormatBytes(p.DownloadedBytes)} / {FormatBytes(p.TotalBytes)}";
                });

                if (p.Status == DownloadStatus.Failed)
                {
                    Console.WriteLine($"错误: {p.ErrorMessage}");
                    DispatcherQueue.EnqueueAsync(() =>
                    {
                        ((ContentDialog)this.Parent).Hide();
                        MessageBox.ShowAsync("抱歉，我们出现了一些错误...\n{p.ErrorMessage}");
                    });
                }
            });

            try
            {
                Directory.CreateDirectory(Path.Combine(Config.CFG_DIR, "UpdateFiles"));
                string outputPath = Path.Combine(Config.CFG_DIR,"UpdateFiles", $"Update_File_{new Random().Next(1000000,9999999)}.exe");

                var cts = new CancellationTokenSource();

                await downloader.DownloadFileAsync(_url, outputPath, progress, cts.Token);
                Console.WriteLine("下载完成！");
                if (File.Exists(outputPath))
                {
                    Process.Start(outputPath);
                    Thread.Sleep(300);
                    Environment.Exit(0);
                }
            }
            catch (OperationCanceledException)
            {
                DispatcherQueue.EnqueueAsync(() =>
                {
                    ((ContentDialog)this.Parent).Hide();
                });
            }
        }
    }
}
