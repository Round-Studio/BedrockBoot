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

namespace BedrockBoot.Standard.Enum.Config;

public enum UIZoom
{
    Percent75,
    Percent100,
    Percent125,
    Percent150,
    Percent200,
    Percent500
}

public static class UIZoomExtensions
{
    public static double ToScale(this UIZoom zoom) => zoom switch
    {
        UIZoom.Percent75 => 0.75,
        UIZoom.Percent100 => 1.00,
        UIZoom.Percent125 => 1.25,
        UIZoom.Percent150 => 1.50,
        UIZoom.Percent200 => 2.00,
        UIZoom.Percent500 => 5.00,
        _ => 1.00
    };

    public static string ToDisplayText(this UIZoom zoom) => zoom switch
    {
        UIZoom.Percent75 => "75%",
        UIZoom.Percent100 => "100%",
        UIZoom.Percent125 => "125%",
        UIZoom.Percent150 => "150%",
        UIZoom.Percent200 => "200%",
        UIZoom.Percent500 => "500%",
        _ => "100%"
    };
}