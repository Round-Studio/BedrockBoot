using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using Octokit;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDownloadAppSdkContent : UserControl
{
    public DialogDownloadAppSdkContent()
    {
        InitializeComponent();
        _ = Update();
    }

    public async Task Update()
    {
        string owner = "BE-Community-Dev";
        string repo = "AppSDKArchive";
        string tag = "1.8";

        try
        {
            var client = new GitHubClient(new ProductHeaderValue("BedrockBoot.Desktop"));
            var release = await client.Repository.Release.Get(owner, repo, tag);

            if (release == null)
            {
                return;
            }

            if (release.Assets != null && release.Assets.Count > 0)
            {
                var downloadUrl = release.Assets[0].BrowserDownloadUrl;
                var downloader = new GithubFilesDownloader();
                var savePath = Path.Combine(PathsList.TempPath, $"sdkinstaller_{Guid.NewGuid()}.exe");
                
                await downloader.DownloadAsync(downloadUrl, savePath, new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ProgressBar.IsIndeterminate)
                            ProgressBar.IsIndeterminate = false;

                        ProgressBar.Value = p.ProgressPercentage;
                        ProgressText.Text = $"下载依赖 {p.ProgressPercentage:F2} %";
                    });
                }));

                if (File.Exists(savePath))
                {
                    await RunAsAdministratorAndWaitAsync(savePath);
                    DialogHost.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            DialogHost.Close();
            DialogHost.Show(new()
            {
                Title = "下载失败",
                Content = ex.Message,
                CloseButtonText = "确定"
            });
        }
    }

    private async Task RunAsAdministratorAndWaitAsync(string filePath)
    {
        Process process = null;
        while (process == null)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                process = Process.Start(processInfo);
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        await process.WaitForExitAsync();
    }
}