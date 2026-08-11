using BedrockBoot.Downloader.Enum.Game;

namespace BedrockBoot.Downloader.Info.Game;

public class VersionBuildInfo
{
    public BuildType GameBuildType { get; set; }
    public GameType GameType { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}