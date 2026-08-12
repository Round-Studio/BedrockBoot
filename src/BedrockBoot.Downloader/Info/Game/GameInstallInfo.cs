using BedrockBoot.Base.Enum.Game;

namespace BedrockBoot.Downloader.Info.Game;

public class GameInstallInfo
{
    public string InstallFolder { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public GameInstallType InstallType { get; set; } = GameInstallType.Tradition;
    public VersionBuildInfo? VersionBuildInfo { get; set; }
}