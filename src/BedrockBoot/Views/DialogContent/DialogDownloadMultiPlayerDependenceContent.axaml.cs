/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Views.Helper;
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
        var isFetchingReleases = false;
        try
        {
            if (GlobalModel.GravityConeClient != null)
            {
                try { GlobalModel.GravityConeClient.Dispose(); } catch { }
                GlobalModel.GravityConeClient = null;
                GlobalModel.CurrentRoomState = null;
            }

            if (!Directory.Exists(EasyTierPath))
                Directory.CreateDirectory(EasyTierPath);
            if (!Directory.Exists(GravityConeExePath))
                Directory.CreateDirectory(GravityConeExePath);

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = I18nManager.Instance["Multiplayer.Dep.Status.FetchingReleases"];
            });

            isFetchingReleases = true;
            var (easyTierRelease, gravityConeRelease) = await GitHubApiClient.ExecuteAsync(async github =>
            {
                var easyTierTask = github.Repository.Release.GetLatest("EasyTier", "EasyTier");
                var gravityConeTask = github.Repository.Release.GetLatest("Tianpao", "GravityCone");

                await Task.WhenAll(easyTierTask, gravityConeTask);

                return (await easyTierTask, await gravityConeTask);
            });
            isFetchingReleases = false;

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
                throw new Exception(string.Format(I18nManager.Instance["Multiplayer.Dep.Error.AssetNotFound.EasyTier"], os));
            if (gravityConeAsset == null)
                throw new Exception(string.Format(I18nManager.Instance["Multiplayer.Dep.Error.AssetNotFound.GravityCone"], os));

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Downloading"];
                EasyTierProgressText.Text = string.Format(I18nManager.Instance["Multiplayer.Dep.Status.DownloadProgress.EasyTier"], 0d);
                GravityConeProgressText.Text = string.Format(I18nManager.Instance["Multiplayer.Dep.Status.DownloadProgress.GravityCone"], 0d);
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
                    EasyTierProgressText.Text = string.Format(I18nManager.Instance["Multiplayer.Dep.Status.DownloadProgress.EasyTier"], p.ProgressPercentage);
                });
            });

            var gravityConeProgress = new Progress<DownloadProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    GravityConeProgressBar.Value = (int)p.ProgressPercentage;
                    GravityConeProgressText.Text = string.Format(I18nManager.Instance["Multiplayer.Dep.Status.DownloadProgress.GravityCone"], p.ProgressPercentage);
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
                StatusText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Extracting"];
                EasyTierProgressText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Extract.EasyTier"];
                GravityConeProgressText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Extract.GravityCone"];
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
                    EasyTierProgressText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Installed.EasyTier"];
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
                    GravityConeProgressText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Installed.GravityCone"];
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
                StatusText.Text = string.Format(I18nManager.Instance["Multiplayer.Dep.Status.DownloadFailed"], ex.Message);
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
                DialogHost.Close();
            });

            if (isFetchingReleases)
                await GitHubErrorDialog.ShowAsync(ex, I18nManager.Instance["GitHub.Error.DependencyDownload"]);
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
            throw new Exception(I18nManager.Instance["Multiplayer.Dep.Error.UnsupportedArchive"]);
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
            StatusText.Text = I18nManager.Instance["Multiplayer.Dep.Status.Completed"];
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