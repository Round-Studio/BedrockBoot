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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Mods.Arke;

namespace BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;

public class ArkeShaderPacksManager
{
    private readonly VersionConfig _instance;

    public ArkeShaderPacksManager(VersionConfig instance)
    {
        _instance = instance;
    }
    
    public string GetShaderPacksDir()
    {
        return Path.Combine(_instance.VersionPath, "config", "BedrockBoot2", "levilamina", "ll.mods",
            "arke", "shaderpacks");
    }
    
    public List<ArkeShaderPackInfo> GetShaderPacks()
    {
        var shaderPacks = new List<ArkeShaderPackInfo>();
        var shaderPacksDir = GetShaderPacksDir();
        if (!Directory.Exists(shaderPacksDir))
        {
            Directory.CreateDirectory(shaderPacksDir);
            return shaderPacks;
        }

        Directory.GetFiles(shaderPacksDir, "*.zip").ToList().ForEach(file =>
        {
            shaderPacks.Add(new()
            {
                PackName = Path.GetFileName(file),
                PackPath = file,
                PackSize = (new FileInfo(file)).Length
            });
        });

        return shaderPacks;
    }
}