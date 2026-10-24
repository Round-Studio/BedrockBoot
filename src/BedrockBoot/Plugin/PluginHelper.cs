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
using System.IO;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Entry;
using Round.SDK.Helper;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Plugin;

public class PluginHelper
{
    public static PackConfig ReadPackConfig(string packFile)
    {
        var extractDir = Path.Combine(PathsList.TempPath,
            FileHashCalculator.CalculateHash(packFile, FileHashCalculator.HashType.MD5));
        ZipHelper.ExtractZipFile(packFile, extractDir);
        var configPath = Path.Combine(extractDir, "pack.json");

        if (!File.Exists(configPath)) throw new FileNotFoundException($"插件包配置文件不存在: {configPath}");

        var config = new ConfigEntity<PackConfig>(configPath, false).Data;

        if (string.IsNullOrEmpty(config.BodyFile)) throw new InvalidOperationException("插件包配置中未指定主体文件");
        if (!string.IsNullOrEmpty(config.PackIconPath))
            config.PackIconPath = Path.Combine(extractDir, "assets", "icon", config.PackIconPath);

        config.PackFolder = extractDir;
        config.PackFile = packFile;

        Console.WriteLine($@"读取插件配置: {config.PackName} v{config.PackVersion}");
        return config;
    }
}