using BedrockBoot.Downloader.Info.Game;

namespace BedrockBoot.Downloader.Game;

public class GameDownloader
{
    private readonly GameInstallInfo _gameInstallInfo;
    public static string UserAgent { get; set; } = "BedrockBoot/GameDownloader";
    public GameDownloader(GameInstallInfo gameInstallInfo)
    {
        _gameInstallInfo = gameInstallInfo;
    }

    public void Install()
    {
        
    }
}