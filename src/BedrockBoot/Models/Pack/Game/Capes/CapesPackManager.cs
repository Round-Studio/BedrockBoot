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

using System;
using System.Collections.Generic;
using System.IO;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Capes;

public class CapesPackManager
{
    private readonly VersionConfig _instance;
    public string CapePacksFolder => Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "capes");

    public CapesPackManager(VersionConfig instance)
    {
        _instance = instance;
    }
    
    public List<CapesPackUnit> LoadPacks()
    {
        var packs = new List<CapesPackUnit>();
        if (!Directory.Exists(CapePacksFolder)) return packs;

        foreach (var folder in Directory.GetDirectories(CapePacksFolder))
        {
            var manifestPath = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            packs.Add(new CapesPackUnit
            {
                SourceManifest = new ConfigEntity<CapePackManifest>(manifestPath),
                Folder = folder,
                Instance = _instance
            });
        }

        return packs;
    }
    
    /// <summary>
    ///     导入披风包，返回导入失败的包文件名列表。
    /// </summary>
    public List<string> ImportPacks(List<string> packs)
    {
        var failed = new List<string>();

        Directory.CreateDirectory(CapePacksFolder);
        foreach (var pack in packs)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(pack);
                var targetDir = Path.Combine(CapePacksFolder, name);
                for (var i = 2; Directory.Exists(targetDir); i++)
                    targetDir = Path.Combine(CapePacksFolder, $"{name} ({i})");

                ZipHelper.ExtractZipFile(pack, targetDir);

                if (File.Exists(Path.Combine(targetDir, "manifest.json"))) continue;

                Directory.Delete(targetDir, true);
                failed.Add(Path.GetFileName(pack));
            }
            catch (Exception)
            {
                failed.Add(Path.GetFileName(pack));
            }
        }

        return failed;
    }
}
