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
using System.Linq;
using BedrockBoot.Base.Entry.Game.Pack.Archive.Export;
using BedrockBoot.Base.Enum.Type.Export;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveExport
{
    private readonly ExportConfig _config;
    public string TempFolder { get; private set; }

    public ArchiveExport(ExportConfig config)
    {
        _config = config;
    }

    public void Export(string outputFile)
    {
        TempFolder = Path.Combine(PathsList.TempPath, $"export_{Guid.NewGuid()}");
        Directory.CreateDirectory(TempFolder);

        Console.WriteLine($@"导出存档的临时目录：{TempFolder}");
        ExportWorld();

        if (_config.ExportType == ArchiveExportType.Template)
        {
            var configFile = new ConfigEntity<TemplateManifest>(Path.Combine(TempFolder, "manifest.json"));
            if (string.IsNullOrEmpty(_config.PackVersion))
            {
                _config.PackVersion = "1.0.0";
            }
            configFile.Data = new();
            configFile.Data.Header = new()
            {
                Name = _config.PackName,
                Description = _config.PackDescription,
                Version = _config.PackVersion.Split('.').Select(int.Parse).ToList(),
                BaseGameVersion = _config.ArchiveInfo!.VersionInfo.Info.Version.Split('.').Select(int.Parse).ToList(),
                Uuid = Guid.NewGuid().ToString(),
                AllowRandomSeed = _config.AllowRandomSeed,
                LockTemplateOptions = _config.LockTemplateOptions
            };
            configFile.Data.Modules = new()
            {
                new()
                {
                    Description = _config.PackDescription,
                    Uuid = Guid.NewGuid().ToString(),
                    Version = _config.PackVersion.Split('.').Select(int.Parse).ToList()
                }
            };
            
            configFile.Save();
            if (_config.AllowRandomSeed)
                Directory.Delete(Path.Combine(TempFolder, "db"), true);

            var resConf =
                new ConfigEntity<List<PackItem>>(Path.Combine(TempFolder, "world_resource_packs.json"), false);
            var behConf =
                new ConfigEntity<List<PackItem>>(Path.Combine(TempFolder, "world_behavior_packs.json"), false);

            var manager = new ResourcePackManager(_config.ArchiveInfo.VersionInfo);
            var packs = manager.GetAllPack();
            foreach (var pack in packs)
            {
                var packFolder = pack.PackRootPath;
                var packFolderName = Path.GetFileName(packFolder);

                if (resConf.Data.Select(x => x.PackId).Contains(pack.Header.Uuid))
                {
                    CopyDirectory(packFolder, Path.Combine(TempFolder, "resource_packs", packFolderName));
                }
                if (behConf.Data.Select(x => x.PackId).Contains(pack.Header.Uuid))
                {
                    CopyDirectory(packFolder, Path.Combine(TempFolder, "behavior_packs", packFolderName));
                }
            }
        }

        ZipHelper.CreateZipFile(TempFolder, outputFile);
    }

    #region 私有方法

    private void ExportWorld()
    {
        CopyDirectory(_config.ArchiveInfo!.Path, TempFolder);
        if (!_config.PortableBedrockBootConfig)
        {
            if (Directory.Exists(Path.Combine(_config.ArchiveInfo.Path, ".bb")))
                Directory.Delete(Path.Combine(_config.ArchiveInfo.Path, ".bb"), true);
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    #endregion
}