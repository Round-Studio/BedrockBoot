using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Downloader.Enum;
using BedrockBoot.Downloader.Event.Progress;
using BedrockBoot.Downloader.Files;
using BedrockBoot.Downloader.Info.Game;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;

namespace BedrockBoot.Downloader.Game;

public class GameDownloader
{
    private readonly GameInstallInfo _gameInstallInfo;
    public static string UserAgent { get; set; } = "BedrockBoot/GameDownloader";
    public IProgress<DownloadGameProgress> DownloadProgress { get; set; } = new Progress<DownloadGameProgress>();
    public Func<List<string>,string> OnChooseDownloadUrl { get; set; } = urls => urls.FirstOrDefault() ?? throw new Exception("未选择下载地址");
    public bool IsCanInstall { get; set; } = true;
    public GameDownloader(GameInstallInfo gameInstallInfo)
    {
        _gameInstallInfo = gameInstallInfo;
    }

    public async Task Install(string url,bool receiveDownloadLock = false)
    {
        PrepareDownloadDirectory();
        if (_gameInstallInfo.InstallType == GameInstallType.Tradition)
        {
            DownloadProgress.Report(new(GameInstallStatus.GetUrl, "获取文件地址", 0));
            var packagePath = Path.Combine(_gameInstallInfo.InstallFolder, "version_save", $"{_gameInstallInfo.VersionBuildInfo!.Version}.insPack");
            // 没直接获取 URL，后面一定要重写这块的逻辑啊！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！

            var downloader = new MultiThreadDownloader();
            await downloader.DownloadAsync(url, packagePath, new Progress<DownloadProgress>(progress =>
            {
                DownloadProgress.Report(new(GameInstallStatus.DownloadFile, $"下载中: {progress.ProgressPercentage:F2}%", progress.ProgressPercentage));
            }));
            
            var md5CheckResult = await CheckMD5(packagePath);
            if(!md5CheckResult)
            {
                DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败", 0));
            }

            if (!string.IsNullOrEmpty(LastComputedMd5))
                GamePackageCacheIndex.Register(_gameInstallInfo.VersionBuildInfo.Version,
                    _gameInstallInfo.VersionBuildInfo.GameBuildType.ToString(),
                    packagePath, LastComputedMd5);
            else
                DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败", 0));
            
            if (receiveDownloadLock)
            {
                while (!IsCanInstall)
                {
                    await Task.Delay(100);
                }
            } // 外部控制是否开始安装

            var installDir = Path.Combine(_gameInstallInfo.InstallFolder,
                GameInfoHelper.GetGameFolderRootName(_gameInstallInfo.InstallFolder), _gameInstallInfo.InstanceName);

            await DownloaderCore.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions
            {
                FileFullPath = packagePath,
                GameName = _gameInstallInfo.InstanceName,
                InstallDstFolder = installDir,
                GameTypeVersion = _gameInstallInfo.VersionBuildInfo.GameType == GameType.Release
                    ?
                    MinecraftGameTypeVersion.Release
                    : _gameInstallInfo.VersionBuildInfo.GameType == GameType.Preview
                        ? MinecraftGameTypeVersion.Preview
                        : MinecraftGameTypeVersion.Beta,
                Type = _gameInstallInfo.VersionBuildInfo.GameBuildType == BuildType.Gdk ? MinecraftBuildTypeVersion.GDK : MinecraftBuildTypeVersion.UWP,
                UseHardwareDecode = GlobalModel.Config.Data.IsUseHardwareDecode,
                ExtractionProgress = new Progress<DecompressProgress>(progress =>
                {
                    DownloadProgress.Report(new(GameInstallStatus.InstallGame, $"解压文件 ({progress.Percentage:F2}%)", progress.Percentage));
                }),
                InstallStates = new Progress<InstallStates>(states =>
                {
                    HandleInstallState(states, installDir);
                })
            });
        }
    }

    #region 别人的私密方法怎么能偷看呢.jpg

    private void SaveVersionConfig(string installDir)
    {
        var conf = new VersionConfig
        {
            VersionPath = installDir,
            Info = new VersionConfig.VersionInfo
            {
                BuildType = _gameInstallInfo.VersionBuildInfo!.GameBuildType,
                Version = _gameInstallInfo.VersionBuildInfo.Id,
                VersionName = _gameInstallInfo.InstanceName,
                VersionType = _gameInstallInfo.VersionBuildInfo.GameType
            }
        };

        GameInfoHelper.SaveVersionConfig(conf);
    }
    private void HandleInstallState(InstallStates state, string installDir)
    {
        switch (state)
        {
            case InstallStates.Extracted:
                if (_gameInstallInfo.VersionBuildInfo!.GameBuildType == BuildType.Gdk)
                {
                    SaveVersionConfig(installDir);
                    DownloadProgress.Report(new(GameInstallStatus.Completed, "安装完成", 100));
                }

                break;

            case InstallStates.Registering:
                if (_gameInstallInfo.VersionBuildInfo!.GameBuildType == BuildType.Gdk) SaveVersionConfig(installDir);
                break;

            case InstallStates.Registered:
                if (_gameInstallInfo.VersionBuildInfo!.GameBuildType == BuildType.Gdk)
                    DownloadProgress.Report(new(GameInstallStatus.Completed, "安装完成", 100));
                break;
        }
    }
    private string? LastComputedMd5 { get; set; }
    private async Task<bool> CheckMD5(string file, CancellationToken token = default, bool showError = true)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var fileMD5 = await ComputeFileMD5.ComputeFileMD5Async(file);
            token.ThrowIfCancellationRequested();

            LastComputedMd5 = fileMD5;

            foreach (var variation in McAppxVersionHelper.Versions
                         .Find(x => x.Key.Replace(".", "") == _gameInstallInfo.VersionBuildInfo.Id.Replace(".", ""))
                         .Variations)
                if (variation.MD5 == fileMD5)
                    return true;

            if (showError) DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败: {fileMD5}", 0));

            return false;
        }
        catch (OperationCanceledException)
        {
            // 取消操作不应被当作校验失败上报
            throw;
        }
        catch (Exception ex)
        {
            if (showError) DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败\n{ex}", 0));
            return false;
        }
    }
    private void PrepareDownloadDirectory()
    {
        var versionSavePath = Path.Combine(_gameInstallInfo.InstallFolder, "version_save");
        if (!Directory.Exists(versionSavePath)) Directory.CreateDirectory(versionSavePath);
    }

    #endregion
}