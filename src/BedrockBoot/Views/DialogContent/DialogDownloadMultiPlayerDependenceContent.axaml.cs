using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Downloader.File;
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

    private readonly bool _navigateAfterComplete;

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

            var os = GetSystemType();

            var easyTierAsset = easyTierRelease.Assets.FirstOrDefault(x => 
                x.Name.Contains($"easytier-{os}-x86_64") && (x.Name.EndsWith(".zip") || x.Name.EndsWith(".tar.gz")));
            
            var gravityConeAsset = gravityConeRelease.Assets.FirstOrDefault(x => 
                (x.Name.ToLower().Contains(os)) && (x.Name.ToLower().Contains("amd64")) &&
                x.Name.Contains("cli") && 
                (x.Name.EndsWith(".zip") || x.Name.EndsWith(".7z") || x.Name.EndsWith(".tar.gz")));

            if (gravityConeAsset == null)
            {
                gravityConeAsset = gravityConeRelease.Assets.FirstOrDefault(x => 
                    (x.Name.ToLower().Contains(os)) && (x.Name.ToLower().Contains("amd64")) &&
                    (x.Name.EndsWith(".zip") || x.Name.EndsWith(".7z") || x.Name.EndsWith(".tar.gz")));
            }

            if (easyTierAsset == null)
                throw new Exception($"未找到 EasyTier {os} 版本下载包");
            if (gravityConeAsset == null)
                throw new Exception($"未找到 GravityCone {os} CLI 版本下载包");

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
                ExtractEasyTier(easyTierDownloadPath, easyTierRelease.TagName);
                
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
                ExtractGravityCone(gravityConeDownloadPath, gravityConeRelease.TagName);
                
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

    private void ExtractEasyTier(string archivePath, string version)
    {
        if (archivePath.EndsWith(".tar.gz"))
        {
            ZipHelper.ExtractTarGz(archivePath, EasyTierPath, true);
        }
        else
        {
            ZipHelper.ExtractZipFile(archivePath, EasyTierPath, true);
        }
        
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        var os = GetSystemType();
        var extractedFolder = Path.Combine(EasyTierPath, $"easytier-{os}-x86_64");
        
        if (Directory.Exists(extractedFolder))
        {
            foreach (var file in Directory.GetFiles(extractedFolder))
            {
                var targetFile = Path.Combine(EasyTierPath, Path.GetFileName(file));
                try
                {
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
                catch { }
            }
            
            try { Directory.Delete(extractedFolder, true); } catch { }
        }

        if (IsLinux())
        {
            var easyTierCli = Path.Combine(EasyTierPath, "easytier-cli");
            var easyTierCore = Path.Combine(EasyTierPath, "easytier-core");
            
            if (File.Exists(easyTierCli))
                SetExecutablePermission(easyTierCli);
            if (File.Exists(easyTierCore))
                SetExecutablePermission(easyTierCore);
        }

        Models.Helper.MultiplayerDependencyHelper.WriteLocalVersion(
            Models.Helper.MultiplayerDependencyHelper.EasyTierVersionFile,
            "EasyTier", version);
    }

    private void ExtractGravityCone(string archivePath, string version)
    {
        if (archivePath.EndsWith(".tar.gz"))
        {
            ZipHelper.ExtractTarGz(archivePath, GravityConeExePath, true);
        }
        else if (archivePath.EndsWith(".zip"))
        {
            ZipHelper.ExtractZipFile(archivePath, GravityConeExePath, true);
        }
        else if (archivePath.EndsWith(".7z"))
        {
            throw new Exception("GravityCone 发布包为 7z 格式，当前不支持解压。请添加 7z 支持或联系开发者改用 zip 格式。");
        }
        else
        {
            ZipHelper.ExtractZipFile(archivePath, GravityConeExePath, true);
        }
        
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        if (IsLinux())
        {
            var gravityConeBin = Path.Combine(GravityConeExePath, "gravitycone");
            if (File.Exists(gravityConeBin))
                SetExecutablePermission(gravityConeBin);
        }

        Models.Helper.MultiplayerDependencyHelper.WriteLocalVersion(
            Models.Helper.MultiplayerDependencyHelper.GravityConeVersionFile,
            "GravityCone", version);
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

                    if (_navigateAfterComplete)
                        MainGravityConePage.NavigationFrame?.NavigateTo(new GravityConeInit());
                });
            });
        }
    }

    private static string GetSystemType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "darwin";
        return "unknown";
    }

    private static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    private static void SetExecutablePermission(string filePath)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch { }
    }
}