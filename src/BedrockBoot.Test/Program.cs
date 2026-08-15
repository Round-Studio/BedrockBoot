using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Downloader;
using BedrockBoot.Downloader.Event.Progress;
using BedrockBoot.Downloader.Game;
using BedrockBoot.Downloader.Info.Game;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;

GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
GlobalModel.Config.Load();

DownloaderCore.InitCore();
var downloader = new GameDownloader(new GameInstallInfo()
{
    InstallFolder = @"I:\ttttttest",
    VersionBuildInfo = new()
    {
        Id = "1.21.132",
        Version = "1.21.13201",
        GameBuildType = BuildType.Gdk,
        GameType = GameType.Release
    },
    InstanceName = "test_1",
    InstallType = GameInstallType.Modern
});

downloader.DownloadProgress = new Progress<DownloadGameProgress>(p =>
{
    Console.WriteLine($"[GameDownloader] {p.Status} {p.ProgressPercentage:F2} % {p.Message}");
});

downloader.OnChooseDownloadUrl=new Func<List<GameDownloadUrlInfo>, GameDownloadUrlInfo>(urls =>
{
    urls.ForEach(x => Console.WriteLine(x.Url));
    return urls[0];
});

await downloader.TraditionGetUrl();
await downloader.Install();