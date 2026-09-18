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

using BedrockBoot.Models.Global;

namespace BedrockBoot.Proton;

public class ProtonNeoCore
{
    public static string ProtonRootPath => Path.Combine(PathsList.NeoProtonPath, "proton", "GDK-Proton-xuser");
    public ProtonNeoCore()
    {
        if (OperatingSystem.IsWindows())
            return;
    }

    public static bool IsInstalledKits()
    {
        var rootPath = PathsList.NeoProtonPath;
        var protonPath = ProtonRootPath;
        var umuPath = Path.Combine(rootPath, "umu");
        var gameFix = Path.Combine(rootPath, "gameFix");

        return Directory.Exists(protonPath) && Directory.Exists(umuPath) && Directory.Exists(gameFix);
    }
}