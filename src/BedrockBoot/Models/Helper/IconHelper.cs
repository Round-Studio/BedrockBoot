using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Type;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Helper;

public class IconHelper
{
    public static string GetGameIconUrl(VersionConfig conf)
    {
        return conf.Info.GameIconType switch
        {
            GameIconType.Customization => string.IsNullOrEmpty(conf.Info.GameIconPath)
                ? conf.Info.VersionType == MinecraftGameTypeVersion.Release
                    ? "avares://BedrockBoot/Assets/Icon/Logo/Grass.png"
                    : "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png"
                : conf.Info.GameIconPath,
            GameIconType.Default => conf.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "avares://BedrockBoot/Assets/Icon/Logo/Grass.png"
                : "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png",
            GameIconType.Grass => "avares://BedrockBoot/Assets/Icon/Logo/Grass.png",
            GameIconType.GrassScript => "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png",
            GameIconType.Worktable => "avares://BedrockBoot/Assets/Icon/Logo/Worktable.png",
            GameIconType.Stone => "avares://BedrockBoot/Assets/Icon/Logo/Stone.png",
            GameIconType.EndlandStone => "avares://BedrockBoot/Assets/Icon/Logo/EndlandStone.png",
            GameIconType.Cs2 => "avares://BedrockBoot/Assets/Icon/Logo/Cs2.png",
            GameIconType.Falcons => "avares://BedrockBoot/Assets/Icon/Logo/Falcons.png",
            _ => conf.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "avares://BedrockBoot/Assets/Icon/Logo/Grass.png"
                : "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png"
        };
    }
}