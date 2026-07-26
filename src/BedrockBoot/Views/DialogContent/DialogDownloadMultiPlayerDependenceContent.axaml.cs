using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.GravityConePage;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.MultiplayerPage;
using Octokit;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDownloadMultiPlayerDependenceContent : UserControl
{
    private static readonly string GravityConePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "GravityCone");
    
    public static readonly string EasyTierPath = Path.Combine(GravityConePath, "easytier");
    public static readonly string GravityConeExePath = Path.Combine(PathsList.PaperConnectPath, "gravitycone");

    private bool _easyTierCompleted = false;
    private bool _gravityConeCompleted = false;

    /// <summary>下载完成后是否导航联机页去初始化（从设置页更新依赖时为 false）</summary>
    private readonly bool _navigateAfterComplete;

    /// <summary>下载安装全部完成后的回调（无论来源）</summary>
    public Action? Completed { get; set; }

    public DialogDownloadMultiPlayerDependenceContent(bool navigateAfterComplete = true)
    {
        _navigateAfterComplete = navigateAfterComplete;
        InitializeComponent();
        
        EasyTierProgressBar.IsIndeterminate = true;
        GravityConeProgressBar.IsIndeterminate = true;
        
        Download();
    }

    public async void Download()
    {
        try
        {
            // 更新场景下 CLI 可能正在运行并锁定 exe 文件，必须先关闭
            if (GlobalModel.GravityConeClient != null)
            {
                try { GlobalModel.GravityConeClient.Dispose(); } catch { }
                GlobalModel.GravityConeClient = null;
                GlobalModel.CurrentRoomState = null;
            }

            var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

            if (!Directory.Exists(EasyTierPath))
                Directory.CreateDirectory(EasyTierPath);
            if (!Directory.Exists(GravityConeExePath))
                Directory.CreateDirectory(GravityConeExePath);

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = "正在获取最新版本信息...";
            });

            var easyTierTask = github.Repository.Release.GetLatest("EasyTier", "EasyTier");
            var gravityConeTask = github.Repository.Release.GetLatest("Tianpao", "GravityCone");
            
            await Task.WhenAll(easyTierTask, gravityConeTask);

            var easyTierRelease = await easyTierTask;
            var gravityConeRelease = await gravityConeTask;

            var easyTierAsset = easyTierRelease.Assets.FirstOrDefault(x => 
                x.Name.Contains("easytier-windows-x86_64") && x.Name.EndsWith(".zip"));
            
            var gravityConeAsset = gravityConeRelease.Assets.FirstOrDefault(x => 
                (x.Name.Contains("windows") || x.Name.Contains("Windows")) && 
                x.Name.Contains("cli") && 
                (x.Name.EndsWith(".zip") || x.Name.EndsWith(".7z")));

            if (gravityConeAsset == null)
            {
                gravityConeAsset = gravityConeRelease.Assets.FirstOrDefault(x => 
                    (x.Name.Contains("windows") || x.Name.Contains("Windows")) && 
                    (x.Name.EndsWith(".zip") || x.Name.EndsWith(".7z")));
            }

            if (easyTierAsset == null)
                throw new Exception("未找到 EasyTier Windows 版本下载包");
            if (gravityConeAsset == null)
                throw new Exception("未找到 GravityCone Windows CLI 版本下载包");

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = "开始并行下载...";
                EasyTierProgressText.Text = $"下载 EasyTier (0%)";
                GravityConeProgressText.Text = $"下载 GravityCone (0%)";
                EasyTierProgressBar.IsIndeterminate = false;
                GravityConeProgressBar.IsIndeterminate = false;
                EasyTierProgressBar.Value = 0;
                GravityConeProgressBar.Value = 0;
            });

            var easyTierFileName = easyTierAsset.Name;
            var gravityConeFileName = gravityConeAsset.Name;
            
            var easyTierDownloadPath = Path.Combine(PathsList.TempPath, easyTierFileName);
            var gravityConeDownloadPath = Path.Combine(PathsList.TempPath, gravityConeFileName);

            var downloader = new GithubFilesDownloader();

            var easyTierProgress = new Progress<DownloadProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    EasyTierProgressBar.Value = (int)p.ProgressPercentage;
                    EasyTierProgressText.Text = $"下载 EasyTier ({p.ProgressPercentage:F1}%)";
                });
            });

            var gravityConeProgress = new Progress<DownloadProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    GravityConeProgressBar.Value = (int)p.ProgressPercentage;
                    GravityConeProgressText.Text = $"下载 GravityCone ({p.ProgressPercentage:F1}%)";
                });
            });

            var easyTierDownloadTask = downloader.DownloadAsync(
                easyTierAsset.BrowserDownloadUrl, 
                easyTierDownloadPath, 
                easyTierProgress);

            var gravityConeDownloadTask = downloader.DownloadAsync(
                gravityConeAsset.BrowserDownloadUrl, 
                gravityConeDownloadPath, 
                gravityConeProgress);

            await Task.WhenAll(easyTierDownloadTask, gravityConeDownloadTask);

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = "下载完成，正在解压...";
                EasyTierProgressText.Text = "解压 EasyTier...";
                GravityConeProgressText.Text = "解压 GravityCone...";
                EasyTierProgressBar.IsIndeterminate = true;
                GravityConeProgressBar.IsIndeterminate = true;
            });

            var easyTierExtractTask = Task.Run(() =>
            {
                ZipHelper.ExtractZipFile(easyTierDownloadPath, EasyTierPath, true);
                if (File.Exists(easyTierDownloadPath))
                    File.Delete(easyTierDownloadPath);

                var folder = Path.Combine(EasyTierPath, "easytier-windows-x86_64");
                Directory.GetFiles(folder).ToList()
                    .ForEach(f =>
                    {
                        try
                        {
                            if (File.Exists(Path.Combine(EasyTierPath, Path.GetFileName(f))))
                                File.Delete(f);
                            File.Copy(f, Path.Combine(EasyTierPath, Path.GetFileName(f)));
                        }
                        catch
                        {
                        }
                    });

                // 记录已安装的版本号，供 设置>通用>软件更新>依赖更新 检查更新使用
                Models.Helper.MultiplayerDependencyHelper.WriteLocalVersion(
                    Models.Helper.MultiplayerDependencyHelper.EasyTierVersionFile,
                    "EasyTier", easyTierRelease.TagName);

                Dispatcher.UIThread.Invoke(() =>
                {
                    EasyTierProgressBar.IsIndeterminate = false;
                    EasyTierProgressBar.Value = 100;
                    EasyTierProgressText.Text = "EasyTier 安装完成";
                    _easyTierCompleted = true;
                    CheckAllCompleted();
                });
            });

            var gravityConeExtractTask = Task.Run(() =>
            {
                ExtractGravityCone(gravityConeDownloadPath, GravityConeExePath);
                if (File.Exists(gravityConeDownloadPath))
                    File.Delete(gravityConeDownloadPath);

                // 记录已安装的版本号，供 设置>通用>软件更新>依赖更新 检查更新使用
                Models.Helper.MultiplayerDependencyHelper.WriteLocalVersion(
                    Models.Helper.MultiplayerDependencyHelper.GravityConeVersionFile,
                    "GravityCone", gravityConeRelease.TagName);

                Dispatcher.UIThread.Invoke(() =>
                {
                    GravityConeProgressBar.IsIndeterminate = false;
                    GravityConeProgressBar.Value = 100;
                    GravityConeProgressText.Text = "GravityCone 安装完成";
                    _gravityConeCompleted = true;
                    CheckAllCompleted();
                });
            });

            await Task.WhenAll(easyTierExtractTask, gravityConeExtractTask);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = $"下载失败: {ex.Message}";
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
                DialogHost.Close();
            });
        }
    }

    private void ExtractGravityCone(string archivePath, string extractPath)
    {
        if (archivePath.EndsWith(".7z"))
        {
            try
            {
                ZipHelper.ExtractZipFile(archivePath, extractPath, true);
            }
            catch
            {
                throw new Exception("GravityCone 发布包为 7z 格式，当前不支持解压。请添加 7z 支持或联系开发者改用 zip 格式。");
            }
        }
        else
        {
            ZipHelper.ExtractZipFile(archivePath, extractPath, true);
        }
    }

    private void CheckAllCompleted()
    {
        if (_easyTierCompleted && _gravityConeCompleted)
        {
            StatusText.Text = "所有依赖下载安装完成";
            StatusText.Foreground = Avalonia.Media.Brushes.Green;
            
            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();

                    Completed?.Invoke();

                    // 从设置页（依赖更新）打开时不重新初始化联机页；
                    // 且联机页可能从未打开过，NavigationFrame 为 null
                    if (_navigateAfterComplete)
                        MainGravityConePage.NavigationFrame?.NavigateTo(new GravityConeInit());
                });
            });
        }
    }
}