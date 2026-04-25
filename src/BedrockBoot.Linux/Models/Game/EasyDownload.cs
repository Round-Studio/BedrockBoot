using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;
using Round.SDK.Helper;
using DownloadProgress = BedrockBoot.Base.Entry.Progress.DownloadProgress;

namespace BedrockBoot.Services;

// 定义一个新的进度信息类，包含下载速度和进度
public class DownloadProgressInfo
{
    public DownloadProgressInfo(double percentage, string speed, long downloadedBytes, long totalBytes)
    {
        Percentage = percentage;
        Speed = speed;
        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
    }

    public double Percentage { get; set; }
    public string Speed { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
}

public class EasyDownload
{
    public EasyDownload(BuildInfo info, bool isUsePack, string dir, string gameName)
    {
        BuildInfo = info;
        InstallFolder = dir;
        GameName = gameName;
        IsUsePack = isUsePack;
    }

    public BuildInfo BuildInfo { get; set; }
    public string InstallFolder { get; set; }
    public string GameName { get; set; }
    public bool IsUsePack { get; set; }

    // 进度报告回调
    public Action<string, DownloadProgressInfo> DownloadProgress { get; set; } // 修改：整合下载进度和速度
    public Action<string, double> MergeProgress { get; set; }
    public Action<string, double> ExtractionProgress { get; set; }
    public Action<string, DeploymentProgress> DeploymentProgress { get; set; }
    public Action<string> StatusText { get; set; }
    public Action<InstallStates> InstallStateChanged { get; set; }
    public Action<string, string, Exception> ErrorOccurred { get; set; }
    public Action<VersionConfig> Completed { get; set; }

    public VersionConfig GameConfig { get; private set; }

    public async Task InstallAsync(string url, CancellationToken token = default)
    {
        Console.WriteLine($@"下载游戏，地址：{url}");
        try
        {
            // 1. 准备下载目录
            PrepareDownloadDirectory();

            // 2. 下载游戏包
            var packagePath = await DownloadPackageAsync(url, token);
            token.ThrowIfCancellationRequested();

            MergeProgress?.Invoke("验证包...", 80);

            // 3. 验证包完整性
            if (!await ValidatePackageAsync(packagePath, token)) return;
            token.ThrowIfCancellationRequested();

            // 4. 标记合并完成
            OnMergeComplete();
            token.ThrowIfCancellationRequested();

            // 5. 安装包
            await InstallPackageAsync(packagePath, token);
            token.ThrowIfCancellationRequested();

            Completed?.Invoke(GameConfig);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke("安装失败", $"游戏 {GameName} 安装失败", ex);
        }
    }

    private void PrepareDownloadDirectory()
    {
        var versionSavePath = Path.Combine(InstallFolder, "version_save");
        if (!Directory.Exists(versionSavePath)) Directory.CreateDirectory(versionSavePath);
    }

    private async Task<string> DownloadPackageAsync(string url, CancellationToken token)
    {
        var packagePath = Path.Combine(InstallFolder, "version_save", $"{BuildInfo.ID}.insPack");

        // 如果文件已存在且MD5校验通过，则跳过下载
        if (File.Exists(packagePath) &&
            await CheckMD5(packagePath, false) &&
            IsUsePack)
        {
            StatusText?.Invoke("使用缓存包");

            // 使用缓存时，报告100%进度
            var progressInfo = new DownloadProgressInfo(100, "使用缓存", 0, 0);
            DownloadProgress?.Invoke("使用缓存包 (100%)", progressInfo);

            OnMergeComplete(); // 使用缓存时也触发合并完成
            return packagePath;
        }

        StatusText?.Invoke("正在下载游戏包...");
        var downloadCount = BedrockBoot.Core.Global.GlobalModel.Config == null ? 4 : BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount;
        var downloader = new MultiThreadDownloader(downloadCount, 1024);
        var speedCalculator = new DownloadSpeedCalculator();

        await downloader.DownloadAsync(url, packagePath, new Progress<DownloadProgress>(progress =>
        {
            // 计算下载速度
            var speedBytes = speedCalculator.UpdateSpeed(progress.DownloadedBytes, progress.TotalBytes);
            var speedFormatted = SizeHelper.FormatBytes(speedBytes);

            // 创建整合的进度信息
            var progressInfo = new DownloadProgressInfo(
                progress.ProgressPercentage,
                speedFormatted,
                progress.DownloadedBytes,
                progress.TotalBytes
            );

            // 更新整合的下载进度
            DownloadProgress?.Invoke($"下载游戏 ({progress.ProgressPercentage:F2}%)", progressInfo);
        }), token);

        return packagePath;
    }

    private async Task<bool> ValidatePackageAsync(string packagePath, CancellationToken token)
    {
        StatusText?.Invoke("正在验证包完整性...");

        if (!await CheckMD5(packagePath))
        {
            ErrorOccurred?.Invoke("无效包", "当前下载的包无效，请重新下载", null);
            return false;
        }

        return true;
    }

    private void OnMergeComplete()
    {
        // 合并完成回调
        MergeProgress?.Invoke("合并完成", 100);
        StatusText?.Invoke("本地安装中...");
    }

    private async Task InstallPackageAsync(string packagePath, CancellationToken token)
    {
        var installDir = Path.Combine(InstallFolder, "bedrock_versions", GameName);

        await CoreGlobal.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions
        {
            FileFullPath = packagePath,
            GameName = GameName,
            InstallDstFolder = installDir,
            GameTypeVersion = BuildInfo.Type,
            Type = BuildInfo.BuildType,
            ExtractionProgress = new Progress<DecompressProgress>(progress =>
            {
                ExtractionProgress?.Invoke($"解压文件 ({progress.Percentage:F2}%)",
                    progress.Percentage);
            }),
            InstallStates = new Progress<InstallStates>(states =>
            {
                InstallStateChanged?.Invoke(states);
                HandleInstallState(states, installDir);
            }),
            CancellationToken = token
        });
    }

    private void HandleInstallState(InstallStates state, string installDir)
    {
        switch (state)
        {
            case InstallStates.Extracted:
                if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
                {
                    SaveVersionConfig(installDir);
                    Completed?.Invoke(GameConfig);
                }

                break;
        }
    }

    private void SaveVersionConfig(string installDir)
    {
        var conf = new VersionConfig
        {
            VersionPath = installDir,
            Info = new VersionConfig.VersionInfo
            {
                BuildType = BuildInfo.BuildType,
                Version = BuildInfo.ID,
                VersionName = GameName,
                VersionType = BuildInfo.Type
            }
        };
        GameConfig = conf;

        GameInfoHelper.SaveVersionConfig(conf);
    }

    public async Task<bool> CheckMD5(string file, bool showError = true)
    {
        try
        {
            var fileMD5 = await ComputeFileMD5.ComputeFileMD5Async(file);

            foreach (var variation in BuildInfo.Variations)
                if (variation.MD5 == fileMD5)
                    return true;

            if (showError) ErrorOccurred?.Invoke("无效包", "当前下载的包无效，请重新下载", null);

            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke("MD5校验失败", "验证文件完整性时出错", ex);
            return false;
        }
    }

    public static async Task<List<GameDownloadUrlInfo>> GetPackageUrls(BuildInfo buildInfo)
    {
        if (buildInfo == null)
            throw new ArgumentNullException(nameof(buildInfo));
        
        if (buildInfo.BuildType != MinecraftBuildTypeVersion.GDK)
            throw new Exception("该游戏在此设备上不支持");
        
        var url = buildInfo.Variations.Find((variation => variation.Arch == Architecture.X64))!.MetaData[0];

        var res = new List<GameDownloadUrlInfo>();
        var uri = new Uri(url);
        var router = uri.AbsolutePath;
        
        SourceList.GameFileDownloadSource.ForEach(s =>
        {
            res.Add(new GameDownloadUrlInfo
            {
                Host = s.Host,
                Url = s.Url.Replace("{router}", router)
            });
        });

        return res;
    }
}