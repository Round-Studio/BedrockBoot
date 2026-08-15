using System.Runtime.InteropServices;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Downloader.Enum;
using BedrockBoot.Downloader.Event.Progress;
using BedrockBoot.Downloader.Files;
using BedrockBoot.Downloader.Game.Cache;
using BedrockBoot.Downloader.Info.Game;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;

namespace BedrockBoot.Downloader.Game;

public class GameDownloader
{
    private readonly GameInstallInfo _gameInstallInfo;
    private string _url = string.Empty;
    public static int MaxFileCount { get; set; } = 256;
    public static int MinFileSize { get; set; } = 20 * 1024 * 1024; // 20mb 以下的文件不进行多线程的下载...
    public static string UserAgent { get; set; } = "BedrockBoot/GameDownloader";
    public IProgress<DownloadGameProgress> DownloadProgress { get; set; } = new Progress<DownloadGameProgress>();

    public Func<List<GameDownloadUrlInfo>, GameDownloadUrlInfo> OnChooseDownloadUrl { get; set; } =
        urls => urls.FirstOrDefault() ?? throw new Exception("未选择下载地址");

    public bool IsCanInstall { get; set; } = true;

    public GameDownloader(GameInstallInfo gameInstallInfo)
    {
        _gameInstallInfo = gameInstallInfo;
    }

    public async Task Install(bool receiveDownloadLock = false)
    {
        if (string.IsNullOrEmpty(_gameInstallInfo.InstanceName))
            _gameInstallInfo.InstanceName = _gameInstallInfo.VersionBuildInfo!.Id;
        PrepareDownloadDirectory();
        var installDir = Path.Combine(_gameInstallInfo.InstallFolder,
            GameInfoHelper.GetGameFolderRootName(_gameInstallInfo.InstallFolder), _gameInstallInfo.InstanceName);
        if (_gameInstallInfo.InstallType == GameInstallType.Tradition)
        {
            var packagePath = Path.Combine(_gameInstallInfo.InstallFolder, "version_save",
                $"{_gameInstallInfo.VersionBuildInfo!.Version}.insPack");
            var isUseCache = false;

            var locFile = GamePackageCacheIndex.Find(_gameInstallInfo.VersionBuildInfo.Version,
                _gameInstallInfo.VersionBuildInfo.GameBuildType.ToString());
            if (locFile != null)
            {
                packagePath = locFile.FilePath;
                isUseCache = true;
            }

            if (string.IsNullOrEmpty(_url) && !isUseCache)
                DownloadProgress.Report(new(GameInstallStatus.Error, "未选择下载源", 0));

            if (!isUseCache)
            {
                DownloadProgress.Report(new(GameInstallStatus.GetUrl, $"<unknown>", 100));
                var downloader = new MultiThreadDownloader();
                await downloader.DownloadAsync(_url, packagePath,
                    new Progress<DownloadProgress>(progress =>
                    {
                        DownloadProgress.Report(new(GameInstallStatus.DownloadFile,
                            $"下载中: {progress.ProgressPercentage:F2}%", progress.ProgressPercentage));
                    }));

                var md5CheckResult = await CheckMD5(packagePath);
                if (!md5CheckResult)
                {
                    DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败", 0));
                }

                if (!string.IsNullOrEmpty(LastComputedMd5))
                    GamePackageCacheIndex.Register(_gameInstallInfo.VersionBuildInfo.Version,
                        _gameInstallInfo.VersionBuildInfo.GameBuildType.ToString(),
                        packagePath, LastComputedMd5);
                else
                    DownloadProgress.Report(new(GameInstallStatus.Error, $"MD5校验失败", 0));
            }
            else
            {
                DownloadProgress.Report(new(GameInstallStatus.DownloadFile, $"下载完毕", 100));
            }

            if (receiveDownloadLock)
            {
                while (!IsCanInstall)
                {
                    await Task.Delay(100);
                }
            }

            SaveVersionConfig(installDir);

            await DownloaderCore.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions
            {
                FileFullPath = packagePath,
                GameName = _gameInstallInfo.InstanceName,
                InstallDstFolder = installDir,
                GameTypeVersion = _gameInstallInfo.VersionBuildInfo.GameType == GameType.Release
                    ? MinecraftGameTypeVersion.Release
                    : _gameInstallInfo.VersionBuildInfo.GameType == GameType.Preview
                        ? MinecraftGameTypeVersion.Preview
                        : MinecraftGameTypeVersion.Beta,
                Type = _gameInstallInfo.VersionBuildInfo.GameBuildType == BuildType.Gdk
                    ? MinecraftBuildTypeVersion.GDK
                    : MinecraftBuildTypeVersion.UWP,
                UseHardwareDecode = GlobalModel.Config.Data.IsUseHardwareDecode,
                ExtractionProgress = new Progress<DecompressProgress>(progress =>
                {
                    DownloadProgress.Report(new(GameInstallStatus.InstallGame, $"解压文件 ({progress.Percentage:F2}%)",
                        progress.Percentage));
                }),
                InstallStates = new Progress<InstallStates>(states => { HandleInstallState(states, installDir); })
            });
        }
        else
        {
            DownloadProgress.Report(new(GameInstallStatus.GetUrl, "获取文件清单", 0));

            var list = await ModernLocalCache.GetVersionFilesAsync(_gameInstallInfo.VersionBuildInfo!.Version);

            var downloadFiles = list.Where(f => !f.IsLocalFile).ToList();
            var localFiles = list.Where(f => f.IsLocalFile).ToList();

            foreach (var file in localFiles)
            {
                var savePath = Path.Combine(installDir, file.Pathname);
                var saveDir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(saveDir) && !Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                File.Copy(file.LocalFile, savePath, true);
            }

            if (downloadFiles.Count == 0)
            {
                DownloadProgress.Report(new(GameInstallStatus.Completed, "安装完成", 100));
                return;
            }

            long totalDownloadBytes = downloadFiles.Sum(f => f.Size);
            long totalDownloadedBytes = 0;
            object progressLock = new();

            using var semaphore = new SemaphoreSlim(MaxFileCount, MaxFileCount);

            var downloadTasks = downloadFiles.Select(async file =>
            {
                var savePath = Path.Combine(installDir, file.Pathname);
                var saveDir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(saveDir) && !Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                await semaphore.WaitAsync();
                try
                {
                    var url = $"{Global.SourceList.BaseUrl}/download/{file.Hashes.Sha256}";
                    long lastFileDownloadedBytes = 0;

                    var fileProgress = new Progress<DownloadProgress>(p =>
                    {
                        lock (progressLock)
                        {
                            long delta = p.DownloadedBytes - lastFileDownloadedBytes;
                            lastFileDownloadedBytes = p.DownloadedBytes;
                            totalDownloadedBytes += delta;

                            double percentage = totalDownloadBytes > 0
                                ? (double)totalDownloadedBytes / totalDownloadBytes * 100
                                : 100;

                            DownloadProgress.Report(new(GameInstallStatus.DownloadFile,
                                $"下载中: {percentage:F2}%", percentage));
                        }
                    });

                    if (file.Size >= MinFileSize)
                    {
                        using var downloader = new MultiThreadDownloader();
                        downloader.AdditionalHeaders ??= new Dictionary<string, string>();
                        downloader.AdditionalHeaders["User-Agent"] = UserAgent;
                        Console.WriteLine($"下载文件 {url}");
                        await downloader.DownloadAsync(url, savePath, fileProgress);
                        Console.WriteLine($"{url} Download OK");
                    }
                    else
                    {
                        var downloader = new SingleThreadDownloader();
                        Console.WriteLine($"下载文件 {url}");
                        await downloader.DownloadAsync(url, savePath, fileProgress);
                        Console.WriteLine($"{url} Download OK");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(downloadTasks);
            DownloadProgress.Report(new(GameInstallStatus.Completed, "安装完成", 100));
        }
    }

    public async Task<bool> TraditionGetUrl()
    {
        try
        {
            var buildInfo = McAppxVersionHelper.GetVersions()
                .Find(x => x.Key.Replace(".", "") == _gameInstallInfo.VersionBuildInfo.Id.Replace(".", ""));

            var url = await DownloaderCore.BedrockCore.GetPackageUri(buildInfo, Architecture.X64);
            Console.WriteLine($@"原始地址：{url}");

            var res = new List<GameDownloadUrlInfo>();
            var uri = new Uri(url);

            if (buildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            {
                var router = uri.AbsolutePath;

                SourceList.GameFileDownloadSource.ForEach(s =>
                {
                    res.Add(new GameDownloadUrlInfo
                    {
                        Host = s.Host,
                        Url = s.Url.Replace("{router}", router)
                    });
                });
            }
            else
            {
                res.Add(new GameDownloadUrlInfo
                {
                    Host = uri.Host,
                    Url = url
                });
            }

            var info = OnChooseDownloadUrl.Invoke(res);
            _url = info.Url;

            return true;
        }
        catch
        {
            return false;
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

            foreach (var variation in McAppxVersionHelper.GetVersions()
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