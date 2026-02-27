using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Win32;

public partial class DownloadVCWindow : Form
{
    public DownloadVCWindow()
    {
        InitializeComponent();

        Task.Run(async () =>
        {
            var tmpFile = Path.Combine(PathsList.TempPath,
                $"vc_redist_{Guid.NewGuid().ToString().Replace("-", "")}.x64.exe");

            var downloader = new MultiThreadDownloader();
            await downloader.DownloadAsync(SourceList.VC20152022Url, tmpFile, new Progress<DownloadProgress>((p =>
            {
                DownloadProgress.Invoke(() =>
                {
                    DownloadProgress.Value = (int)p.ProgressPercentage;
                });

                Console.WriteLine($@"VC 下载进度: {p.ProgressPercentage}");
            })));
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = tmpFile,
                Arguments = "/install /quiet /norestart",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process process = Process.Start(psi);
            process.WaitForExit();
            
            // 重启本体 - 直接获取当前执行程序路径
            RestartMainApplication();

            this.Invoke(Close);
        });
    }
    
    private void RestartMainApplication()
    {
        string mainExePath = Application.ExecutablePath;
            
        // 启动本体
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = mainExePath,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://docs.roundstudio.top/docs/product/bb/commonQuestion",
            UseShellExecute = true
        });
    }

    private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://docs.roundstudio.top/docs/product/bb",
            UseShellExecute = true
        });
    }
}