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
            CapeFileType.Common => "普通",
            CapeFileType.Uncommon => "不常见",
            CapeFileType.Rare => "稀有",
            CapeFileType.Epic => "史诗",
            CapeFileType.Legendary => "传奇",
            _ => "未知"
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