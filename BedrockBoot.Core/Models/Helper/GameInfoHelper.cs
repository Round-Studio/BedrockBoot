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
using BedrockBoot.Standard.Entity;
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
        var folder = FindGameFolder(gameFolder);
        if (folder?.GameFolderType == GameFolderType.PCLSE ||
            string.Equals(folder?.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase))
            return GetPclSeRootName(gameFolder);

        return folder?.GameFolderType == GameFolderType.BedrockBoot
            ? "bedrock_versions"
            : "versions";
    }

    /// <summary>
    /// 获取版本配置列表
    /// </summary>
    public static List<VersionConfig> GetVersionConfigs(string gameFolder)
    {
        var folderType = GetVersionRootFolderType(gameFolder);
        var isPclSe = folderType == GameFolderType.PCLSE ||
                      GlobalModel.Config.Data.GameFolders.Any(folder =>
                          IsSamePath(folder.GameFolderPath, gameFolder) &&
                          string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase));
        var rootName = GetGameFolderRootName(gameFolder);
        var bedrockVersionsPath = string.IsNullOrEmpty(rootName)
            ? gameFolder
            : Path.Combine(gameFolder, rootName);

        if (!Directory.Exists(bedrockVersionsPath))
            return new List<VersionConfig>();

        // 使用 EnumerateDirectories 提高大目录下的性能。
        // 物化目录列表：保留原始顺序作为索引
        var dirs = Directory.EnumerateDirectories(bedrockVersionsPath).ToList();
        if (isPclSe)
        {
            var extractedPath = dirs.FirstOrDefault(dir =>
                string.Equals(Path.GetFileName(dir), "extracted", StringComparison.OrdinalIgnoreCase));
            if (extractedPath != null)
            {
                dirs.Remove(extractedPath);
                dirs.AddRange(Directory.EnumerateDirectories(extractedPath));
            }
        }

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
            if (slots[i] is { } config)
                result.Add(config);

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
                var innerManifestPath = Path.Combine(gamePath, "inner", "appxmanifest.xml");
                if (File.Exists(innerManifestPath)) manifestPath = innerManifestPath;
                else return null;
            }

            // 初始化新配置
            Directory.CreateDirectory(configDir);
            configEntity = new ConfigEntity<VersionConfig>(configJsonPath);
            configEntity.Load();

            var manifest = PackageIdentity.ParseFromXml(File.ReadAllText(manifestPath));
            var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? gamePath;

            configEntity.Data.Info = new VersionConfig.VersionInfo
            {
                Version = manifest.Version,
                VersionName = Path.GetFileName(gamePath),
                BuildType = File.Exists(Path.Combine(gamePath, "MicrosoftGame.Config")) ||
                            File.Exists(Path.Combine(manifestDirectory, "MicrosoftGame.Config"))
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
        data.VersionsRootPath = GetVersionsRootPath(gamePath);
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
        // 仅搜索顶级目录，避免递归产生的性能消耗。
        // 用 FirstOrDefault 提前终止，无需物化为 List
        var exeFile = Directory.EnumerateFiles(gamePath, "Minecraft*.exe").FirstOrDefault();
        if (exeFile == null && GetVersionRootFolderType(gamePath) == GameFolderType.PCLSE)
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

        var configDir = Path.Combine(config.VersionPath!, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        var cfg = new ConfigEntity<VersionConfig>(configJsonPath) { Data = config };
        cfg.Save();
    }

    public static GameFolderType GetVersionRootFolderType(VersionConfig? config)
    {
        if (config == null || string.IsNullOrEmpty(config.VersionPath)) return GameFolderType.Other;

        var folder = FindGameFolder(config.VersionPath);
        if (folder?.GameFolderType == GameFolderType.PCLSE ||
            string.Equals(folder?.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase))
            return GameFolderType.PCLSE;
        return folder?.GameFolderType ?? GameFolderType.Other;
    }

    private static GameFolderType GetVersionRootFolderType(string versionPath)
    {
        var folder = FindGameFolder(versionPath);
        if (folder == null) return GameFolderType.Other;
        if (folder.GameFolderType == GameFolderType.PCLSE ||
            string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase))
            return GameFolderType.PCLSE;
        return folder.GameFolderType;
    }

    private static string GetVersionsRootPath(string gamePath)
    {
        var folder = FindGameFolder(gamePath);
        if (folder != null && (folder.GameFolderType == GameFolderType.PCLSE ||
                               string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase)))
            return Path.GetFullPath(folder.GameFolderPath);

        return Path.GetFullPath(Path.Combine(gamePath, @"..\.."));
    }

    private static string GetPclSeRootName(string gameFolder)
    {
        var fullPath = Path.GetFullPath(gameFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(fullPath);
        if (string.Equals(leaf, "versions", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(leaf, "bedrock_versions", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        foreach (var rootName in new[] { "versions", "bedrock_versions" })
            if (Directory.Exists(Path.Combine(fullPath, rootName)))
                return rootName;

        var siblingLegacy = Path.Combine(fullPath, "..", "bedrock_versions");
        if (Directory.Exists(siblingLegacy)) return Path.Combine("..", "bedrock_versions");

        return string.Empty;
    }

    private static GameFolderInfo? FindGameFolder(string path)
    {
        var normalizedPath = NormalizePath(path);
        return GlobalModel.Config.Data.GameFolders
            .Where(folder => IsSameOrSubPath(normalizedPath, NormalizePath(folder.GameFolderPath)))
            .OrderByDescending(folder => NormalizePath(folder.GameFolderPath).Length)
            .FirstOrDefault();
    }

    private static bool IsSamePath(string left, string right) =>
        string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static bool IsSameOrSubPath(string path, string root)
    {
        if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase)) return true;
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        try
        {
            var normalized = Path.GetFullPath(path);
            var root = Path.GetPathRoot(normalized);
            return string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase)
                ? normalized
                : normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
