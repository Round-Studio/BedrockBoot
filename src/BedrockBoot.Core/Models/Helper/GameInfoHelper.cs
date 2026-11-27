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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Core.Global;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Core.Models.Helper;

public static class GameInfoHelper
{
    private const string ConfigSubPath = "config/BedrockBoot2";
    private const string ConfigFileName = "config.json";
    private const string IndexFileName = "index.json";

    public static string GetGameFolderRootName(string gameFolder)
    {
        var type = GlobalModel.Config.Data.GameFolders.Find(x => x.GameFolderPath == gameFolder)?.GameFolderType ??
                   GameFolderType.BedrockBoot;
        if (type == GameFolderType.BedrockBoot) return "bedrock_versions";

        // PCLSE normally stores versions below <data-root>\versions, while its
        // external-folder setting points directly at the versions directory.
        // Returning an empty suffix lets both layouts be imported unchanged.
        if (type == GameFolderType.PCLSE)
        {
            if (Directory.Exists(Path.Combine(gameFolder, "versions"))) return "versions";
            if (Directory.Exists(Path.Combine(gameFolder, "bedrock_versions"))) return "bedrock_versions";

            // 兼容旧版 PCLSE 已登记的 .minecraft\\bedrock 根目录，
            // 旧版本目录可能位于同级的 .minecraft\\bedrock_versions。
            var siblingLegacy = Path.Combine(gameFolder, "..", "bedrock_versions");
            if (Directory.Exists(siblingLegacy)) return Path.Combine("..", "bedrock_versions");
            return string.Empty;
        }

        var bedrockVersionsPath = "versions";

        return bedrockVersionsPath;
    }

    /// <summary>
    /// 获取版本配置列表
    /// </summary>
    public static List<VersionConfig> GetVersionConfigs(string gameFolder)
    {
        var bedrockVersionsPath = Path.Combine(gameFolder, GetGameFolderRootName(gameFolder));

        if (!Directory.Exists(bedrockVersionsPath))
            return new List<VersionConfig>();

        // 使用 EnumerateDirectories 提高大目录下的性能。
        // PCLSE 的已解包版本位于 versions\extracted\<版本号>，一并加入扫描列表。
        var dirs = Directory.EnumerateDirectories(bedrockVersionsPath)
            .Where(dir => !string.Equals(Path.GetFileName(dir), "extracted", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var extractedPath = Path.Combine(bedrockVersionsPath, "extracted");
        if (Directory.Exists(extractedPath))
            dirs.AddRange(Directory.EnumerateDirectories(extractedPath));

        // 预分配按原始顺序的槽位数组：每个并行任务写入自己的 index，
        // 最终按顺序遍历即可保证与目录枚举顺序一致
        var slots = new VersionConfig?[dirs.Count];
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, dirs.Count, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount)
        }, i =>
        {
            try
            {
                var config = GetVersionConfig(dirs[i]);
                if (config?.Info != null &&
                    !string.IsNullOrEmpty(config.Info.VersionName) &&
                    !string.IsNullOrEmpty(config.Info.Version))
                {
                    slots[i] = config;
                }
            }
            catch (Exception ex)
            {
                errors.Add($@"Read {dirs[i]}: {ex.Message}");
            }
        });

        foreach (var err in errors) Console.WriteLine(err);

        var result = new List<VersionConfig>(slots.Length);
        for (var i = 0; i < slots.Length; i++)
            if (slots[i] != null)
                result.Add(slots[i]);

        Console.WriteLine($@"共获取到 {result.Count} 个实例");
        return result;
    }

    /// <summary>
    /// 获取版本配置列表 (异步版本)
    /// </summary>
    public static async Task<List<VersionConfig>> GetVersionConfigsAsync(string gameFolder)
    {
        return await Task.Run(() => GetVersionConfigs(gameFolder));
    }

    public static VersionConfig GetVersionConfig(string gamePath) => GetVersionConfig(gamePath, false);

    /// <summary>
    /// 获取单个版本的详细配置
    /// </summary>
    public static VersionConfig GetVersionConfig(string gamePath, bool isDebug = false)
    {
        var configDir = Path.Combine(gamePath, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        ConfigEntity<VersionConfig> configEntity;

        // 检查配置文件是否存在
        if (!File.Exists(configJsonPath))
        {
            var manifestPath = Path.Combine(gamePath, "appxmanifest.xml");
            if (!File.Exists(manifestPath))
            {
                // PCLSE 解包的安装包会多一层 inner 目录。
                var innerManifestPath = Path.Combine(gamePath, "inner", "appxmanifest.xml");
                if (File.Exists(innerManifestPath)) manifestPath = innerManifestPath;
                else return null;
            }

            // 初始化新配置
            Directory.CreateDirectory(configDir);
            configEntity = new ConfigEntity<VersionConfig>(configJsonPath);
            configEntity.Load();

            var manifest = PackageIdentity.ParseFromXml(File.ReadAllText(manifestPath));

            configEntity.Data.Info = new VersionConfig.VersionInfo
            {
                Version = manifest.Version,
                VersionName = Path.GetFileName(gamePath),
                BuildType = File.Exists(Path.Combine(gamePath, "MicrosoftGame.Config"))
                    ? MinecraftBuildTypeVersion.GDK
                    : MinecraftBuildTypeVersion.UWP,
                VersionType = GetVersionTypeWithPackName(manifest.Name)
            };
            configEntity.Save();
        }
        else
        {
            configEntity = new ConfigEntity<VersionConfig>(configJsonPath, false);
            configEntity.Load();
        }

        // 绑定运行时路径
        var bodyFile = GetBodyFile(gamePath);
        if (string.IsNullOrEmpty(bodyFile) && !isDebug) return null;

        var data = configEntity.Data;
        data.VersionPath = gamePath;
        data.VersionsRootPath = Path.GetFullPath(Path.Combine(gamePath, @"..\.."));
        data.BodyFile = bodyFile;

        if (data.Config.IsVersionIsolated && OperatingSystem.IsLinux())
        {
            data.Config.IsVersionIsolated = false;
            SaveVersionConfig(data);
        }

        data.GameTags = new()
        {
            $"Minecraft {data.Info.Version}",
            $"Win{data.Info.BuildType}",
            $"{data.Info.VersionType} Version"
        };
        if (IsInstallModLoader(gamePath))
            data.GameTags.Add("LeviLamina");

        return data;
    }

    private static bool IsInstallModLoader(string path)
    {
        var preLoadFile = Path.Combine(path, "config", "BedrockBoot2", "levilamina", "preloader",
            "bin", "PreLoader.dll");
        var bedrockRuntime = Path.Combine(path, "config", "BedrockBoot2", "levilamina",
            "bedrock_runtime_data");
        var levilaminaMainFile = Path.Combine(path, "config", "BedrockBoot2", "levilamina",
            "ll.mods", "LeviLamina", "LeviLamina.dll");

        return File.Exists(preLoadFile) && File.Exists(bedrockRuntime) && File.Exists(levilaminaMainFile);
    }

    /// <summary>
    /// 寻找 Minecraft 执行文件
    /// </summary>
    public static string GetBodyFile(string gamePath)
    {
        // 优先搜索顶级目录；PCLSE 的解包目录可能把文件放在 inner 中。
        var exeFile = Directory.EnumerateFiles(gamePath, "Minecraft*.exe").FirstOrDefault();
        if (exeFile == null)
        {
            var innerPath = Path.Combine(gamePath, "inner");
            if (Directory.Exists(innerPath))
                exeFile = Directory.EnumerateFiles(innerPath, "Minecraft*.exe").FirstOrDefault();
        }

        if (exeFile == null) return string.Empty;

        return Path.GetRelativePath(gamePath, exeFile);
    }

    public static MinecraftGameTypeVersion GetVersionTypeWithPackName(string packName)
    {
        if (string.IsNullOrEmpty(packName)) return MinecraftGameTypeVersion.Release;

        // 使用 Contains 的 StringComparison 忽略大小写，效率更高
        if (packName.Contains("preview", StringComparison.OrdinalIgnoreCase) ||
            packName.Contains("beta", StringComparison.OrdinalIgnoreCase))
        {
            return MinecraftGameTypeVersion.Preview;
        }

        return MinecraftGameTypeVersion.Release;
    }

    public static void SaveVersionConfig(VersionConfig config)
    {
        if (config == null) return;

        var configDir = Path.Combine(config.VersionPath, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        var cfg = new ConfigEntity<VersionConfig>(configJsonPath) { Data = config };
        cfg.Save();
    }

    public static GameFolderType GetVersionRootFolderType(VersionConfig? config)
    {
        if (config == null || string.IsNullOrEmpty(config.VersionPath)) return GameFolderType.Other;

        var gameFolder = GlobalModel.Config.Data.GameFolders
            .Find(x => config.VersionPath.StartsWith(x.GameFolderPath, StringComparison.OrdinalIgnoreCase));

        return gameFolder?.GameFolderType ?? GameFolderType.Other;
    }
}
