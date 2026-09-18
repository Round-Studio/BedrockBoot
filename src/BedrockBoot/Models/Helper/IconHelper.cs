/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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