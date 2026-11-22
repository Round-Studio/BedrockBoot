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

using BedrockBoot.Localization;

namespace BedrockBoot.Standard.Entity.Game.Pack.Capes;

public class CapeFileInfo
{
    public string CapeImagePath { get; set; }
    public string CapeName { get; set; }
    public CapeFileType Type { get; set; }
}

public enum CapeFileType
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public class CapeTypeHelper
{
    public static string GetTypeName(CapeFileType type)
    {
        return type switch
        {
            CapeFileType.Common => Localizer.Get("Cape.Quality.Common"),
            CapeFileType.Uncommon => Localizer.Get("Cape.Quality.Uncommon"),
            CapeFileType.Rare => Localizer.Get("Cape.Quality.Rare"),
            CapeFileType.Epic => Localizer.Get("Cape.Quality.Epic"),
            CapeFileType.Legendary => Localizer.Get("Cape.Quality.Legendary"),
            _ => Localizer.Get("Cape.Quality.Unknown")
        };
    }

    public static CapeFileType GetNameToType(string rarity)
    {
        return rarity.ToLower() switch
        {
            "common" => CapeFileType.Common,
            "uncommon" => CapeFileType.Uncommon,
            "rare" => CapeFileType.Rare,
            "epic" => CapeFileType.Epic,
            "legendary" => CapeFileType.Legendary,
            _ => CapeFileType.Common
        };
    }
}