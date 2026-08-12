using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Downloader.Info.Game;
using BedrockLauncher.Core;

namespace BedrockBoot.Downloader.Game;

public class VersionHelper
{
    public static List<VersionBuildInfo> GetVersionBuildInfoList(GameInstallType type = GameInstallType.Tradition)
    {
        if (type == GameInstallType.Tradition)
        {
            var versions = McAppxVersionHelper.GetVersions();
            return versions.Select(v => new VersionBuildInfo
            {
                GameBuildType = v.BuildType == MinecraftBuildTypeVersion.GDK ? BuildType.Gdk : BuildType.Uwp,
                GameType = v.Type == MinecraftGameTypeVersion.Release ? GameType.Release :
                    v.Type == MinecraftGameTypeVersion.Preview ? GameType.Preview : GameType.Beta,
                Id = v.ID,
                Version = v.Key
            }).ToList();
        }
        else
        {
            return ModernVersionHelper.GetManifest().VersionsArray?.Select(v => new VersionBuildInfo
            {
                GameBuildType = v.BuildType == "GDK" ? BuildType.Gdk : BuildType.Uwp,
                GameType = v.Type == "Release" ? GameType.Release :
                    v.Type == "Preview" ? GameType.Preview : GameType.Beta,
                Id = v.Vid,
                Version = v.Version
            }).ToList()!;
        }
    }
}