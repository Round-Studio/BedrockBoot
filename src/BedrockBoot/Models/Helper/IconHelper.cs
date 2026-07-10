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
                    ? "avares://BedrockBoot/Assets/Icon/Minecraft/Grass.png"
                    : "avares://BedrockBoot/Assets/Icon/Minecraft/GrassScript.png"
                : conf.Info.GameIconPath,
            GameIconType.Default => conf.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "avares://BedrockBoot/Assets/Icon/Minecraft/Grass.png"
                : "avares://BedrockBoot/Assets/Icon/Minecraft/GrassScript.png",
            GameIconType.Grass => "avares://BedrockBoot/Assets/Icon/Minecraft/Grass.png",
            GameIconType.GrassScript => "avares://BedrockBoot/Assets/Icon/Minecraft/GrassScript.png",
            GameIconType.Worktable => "avares://BedrockBoot/Assets/Icon/Minecraft/Worktable.png",
            GameIconType.Stone => "avares://BedrockBoot/Assets/Icon/Minecraft/Stone.png",
            GameIconType.EndlandStone => "avares://BedrockBoot/Assets/Icon/Minecraft/EndlandStone.png",
            _ => conf.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "avares://BedrockBoot/Assets/Icon/Minecraft/Grass.png"
                : "avares://BedrockBoot/Assets/Icon/Minecraft/GrassScript.png"
        };
    }
}