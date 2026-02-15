using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.MultiplayerPage;
using Octokit;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDownloadMultiPlayerDependenceContent : UserControl
{
    public DialogDownloadMultiPlayerDependenceContent()
    {
        InitializeComponent();
        Download();
    }

    public void Download()
    {
        Task.Run(async () =>
        {
            // 创建客户端
            var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

            // 获取指定仓库的所有发布
            var owner = "EasyTier";
            var repo = "EasyTier";
            var releases = (await github.Repository.Release.GetLatest(owner, repo)).Assets.First(x => x.Name.Contains("easytier-windows-x86_64"));

            var url = releases.BrowserDownloadUrl;
            var downloader = new GithubFilesDownload();
            await downloader.DownloadAsync(url, Path.Combine(PathsList.TempPath, releases.Name),
                new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ProgressBar.IsIndeterminate)
                            ProgressBar.IsIndeterminate = false;

                        ProgressBar.Value = (int)p.ProgressPercentage;
                        ProgressText.Text = $"下载依赖 {p.ProgressPercentage:F2} %";
                    });
                }));

            ZipHelper.ExtractZipFile(Path.Combine(PathsList.TempPath, releases.Name), PathsList.EasyTierPath, true);
            
            Dispatcher.UIThread.Invoke(DialogHost.Close);
            Dispatcher.UIThread.Invoke(()=>MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoot()));
        });
    }
}