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

    /// <summary>创建披风包：包 UUID 随机生成（36 位标准格式），包键（语言文件 key 头，
    /// 同时用作 cape.json 的 localization_name 与包目录名）取 8 位随机 uuid；
    /// 包显示名写入 manifest.header.name 与语言文件 capepack.&lt;包键&gt;。</summary>
    public CapesPackUnit CreatePack(string displayName, string? author = null)
    {
        Directory.CreateDirectory(CapePacksFolder);

        string packKey;
        string folder;
        do
        {
            packKey = Guid.NewGuid().ToString("N")[..8];
            folder = Path.Combine(CapePacksFolder, packKey);
        } while (Directory.Exists(folder));

        Directory.CreateDirectory(folder);

        var manifest = new CapePackManifest
        {
            FormatVersion = 1,
            Header = new CapePackHeader
            {
                Name = displayName,
                Uuid = Guid.NewGuid().ToString(),
                Version = new List<int> { 1, 0, 0 }
            },
            Modules = new List<CapePackModule>
            {
                new()
                {
                    Type = "cape_pack",
                    Uuid = Guid.NewGuid().ToString(),
                    Version = new List<int> { 1, 0, 0 }
                }
            },
            Metadata = new CapePackMetadata
            {
                Authors = string.IsNullOrWhiteSpace(author)
                    ? new List<string>()
                    : new List<string> { author.Trim() }
            }
        };
        new ConfigEntity<CapePackManifest>(Path.Combine(folder, "manifest.json")) { Data = manifest }.Save();

        var index = new CapeIndex
        {
            SerializeName = packKey,
            LocalizationName = packKey,
            Capes = new List<CapeDefinition>()
        };
        new ConfigEntity<CapeIndex>(Path.Combine(folder, "cape.json")) { Data = index }.Save();

        Directory.CreateDirectory(Path.Combine(folder, "texts"));
        File.WriteAllText(Path.Combine(folder, "texts", "zh_CN.lang"), $"capepack.{packKey}={displayName}\n");

        return new CapesPackUnit
        {
            SourceManifest = new ConfigEntity<CapePackManifest>(Path.Combine(folder, "manifest.json")),
            Folder = folder,
            Instance = _instance
        };
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
