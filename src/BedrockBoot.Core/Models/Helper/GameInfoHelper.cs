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
                // PCLSE 的实例可能位于 versions\extracted 或外置 versions 目录中，
                // 必须把已注册的数据根目录传下去，否则 VersionsRootPath 会推导成版本目录。
                var config = GetVersionConfig(dirs[i], false, isPclSe ? gameFolder : null);
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
    public static VersionConfig GetVersionConfig(string gamePath, bool isDebug = false) =>
        GetVersionConfig(gamePath, isDebug, null);

    /// <summary>
    /// 获取单个版本的详细配置
    /// </summary>
    /// <param name="gamePath">版本目录</param>
    /// <param name="isDebug">调试模式下允许缺少可执行文件</param>
    /// <param name="gameFolder">已注册的 PCLSE 数据根目录，用于修正 VersionsRootPath</param>
    public static VersionConfig GetVersionConfig(string gamePath, bool isDebug, string? gameFolder)
    {
        // 目录不存在（例如实例已被删除、解压目录还没落地）时直接放弃，
        // 后面的 EnumerateFiles 会抛 DirectoryNotFoundException
        if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath)) return null;

        var configDir = Path.Combine(gamePath, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        // isSave: false —— 读取阶段一律不落盘。
        // 旧实现在这里会先写一份「默认配置」再去解析清单，一旦解析失败就会在磁盘上
        // 留下没有 info 段的 config.json，之后每次读取都在取 data.Info.* 时抛 NRE。
        var configEntity = new ConfigEntity<VersionConfig>(configJsonPath, false);
        var needSave = !File.Exists(configJsonPath);

        var data = configEntity.Data ??= new VersionConfig();

        // JSON 里显式的 "config": null 会把属性初始化器建的默认值覆盖成 null
        data.Config ??= new VersionConfig.VersionConfigEntry();
        data.PlayerData ??= new VersionConfig.PlayerDataEntry();
        data.VersionStatus ??= new VersionConfig.VersionStatusEntry();

        // info 缺失（配置被写坏 / 手写配置）时尝试用清单重建；
        // 重建不出来说明这个目录还不是有效实例（多半是解压到一半），交给下次刷新
        if (IsVersionInfoIncomplete(data.Info))
        {
            // PCLSE 解压版本的清单可能位于 inner 中；TryCreateVersionInfo 会同时检查外层与 inner，
            // 并据此判断 GDK/UWP，这里只负责补全 info 并标记需要回写。
            var rebuilt = TryCreateVersionInfo(gamePath);
            if (rebuilt == null) return null;

            data.Info = rebuilt;
            needSave = true;
        }

        // 绑定运行时路径
        var bodyFile = GetBodyFile(gamePath);
        if (string.IsNullOrEmpty(bodyFile) && !isDebug) return null;

        data.VersionPath = gamePath;
        data.VersionsRootPath = GetVersionsRootPath(gamePath, gameFolder);
        data.BodyFile = bodyFile;

        if (data.Config.IsVersionIsolated && OperatingSystem.IsLinux())
        {
            data.Config.IsVersionIsolated = false;
            needSave = true;
        }

        data.GameTags = new()
        {
            $"Minecraft {data.Info.Version}",
            $"Win{data.Info.BuildType}",
            $"{data.Info.VersionType} Version"
        };
        if (IsInstallModLoader(gamePath))
            data.GameTags.Add("LeviLamina");

        if (needSave) SaveVersionConfig(data);

        return data;
    }

    /// <summary>
    /// info 是否不可用。Version / VersionName 是实例列表、启动参数、标签的基础字段，
    /// 任一为空都算不完整，不能拿去拼字符串。
    /// </summary>
    private static bool IsVersionInfoIncomplete(VersionConfig.VersionInfo? info)
    {
        return info == null ||
               string.IsNullOrEmpty(info.Version) ||
               string.IsNullOrEmpty(info.VersionName);
    }

    /// <summary>
    /// 从安装目录重建 info：
    /// UWP 走 appxmanifest.xml（PCLSE 解压版本的清单可能位于 inner）；GDK 包没有清单，
    /// 退化用文件夹名当版本号（与下载器写入的规则一致）。
    /// 任何读取/解析异常都返回 null —— 解压过程中清单可能只写了一半，不能把异常往上抛，
    /// 更不能留下残缺配置。
    /// </summary>
    private static VersionConfig.VersionInfo? TryCreateVersionInfo(string gamePath)
    {
        // PCLSE 的解压版本会把清单放在 inner 子目录里，外层与 inner 都要尝试。
        var manifestPath = Path.Combine(gamePath, "appxmanifest.xml");
        if (!File.Exists(manifestPath))
        {
            var innerManifestPath = Path.Combine(gamePath, "inner", "appxmanifest.xml");
            if (File.Exists(innerManifestPath)) manifestPath = innerManifestPath;
        }

        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? gamePath;

        // 解压后的 GDK 安装会把 MicrosoftGame.Config 和清单一同放进 inner，
        // 只检查外层目录会把它误判成 UWP，导致下游启动与数据路径使用错误的构建类型。
        var isGdk = HasGdkConfig(gamePath, manifestDirectory);

        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = PackageIdentity.ParseFromXml(File.ReadAllText(manifestPath));
                if (manifest != null && !string.IsNullOrEmpty(manifest.Version))
                    return new VersionConfig.VersionInfo
                    {
                        Version = manifest.Version,
                        VersionName = Path.GetFileName(gamePath),
                        BuildType = isGdk ? MinecraftBuildTypeVersion.GDK : MinecraftBuildTypeVersion.UWP,
                        VersionType = GetVersionTypeWithPackName(manifest.Name)
                    };

                Console.WriteLine($@"清单缺少 Identity 节点：{manifestPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"解析清单失败：{manifestPath} - {ex.Message}");
            }
        }

        if (!isGdk) return null;

        var folderName = Path.GetFileName(gamePath);
        if (string.IsNullOrEmpty(folderName)) return null;

        return new VersionConfig.VersionInfo
        {
            Version = folderName,
            VersionName = folderName,
            BuildType = MinecraftBuildTypeVersion.GDK,
            VersionType = GetVersionTypeWithPackName(folderName)
        };
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
    /// 寻找 Minecraft 执行文件（相对版本目录）
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

    /// <summary>
    /// 获取启动游戏时应使用的工作目录。
    /// PCLSE 的解压版本可能把可执行文件放在 inner 子目录中，此时工作目录必须是可执行文件
    /// 所在的目录，而不是版本根目录，否则游戏无法解析同目录下的运行时文件。
    /// </summary>
    public static string GetGameWorkingDirectory(string? versionPath, string? bodyFile)
    {
        if (string.IsNullOrWhiteSpace(versionPath)) return string.Empty;
        if (string.IsNullOrWhiteSpace(bodyFile)) return versionPath;

        try
        {
            var bodyPath = Path.IsPathRooted(bodyFile) ? bodyFile : Path.Combine(versionPath, bodyFile);
            var directory = Path.GetDirectoryName(bodyPath);
            return string.IsNullOrEmpty(directory) ? versionPath : directory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取工作目录失败（{versionPath}）：{ex.Message}");
            return versionPath;
        }
    }

    /// <summary>
    /// 获取启动游戏时应使用的工作目录。
    /// </summary>
    public static string GetGameWorkingDirectory(VersionConfig? config) =>
        config == null ? string.Empty : GetGameWorkingDirectory(config.VersionPath, config.BodyFile);

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
        if (config == null || string.IsNullOrEmpty(config.VersionPath)) return;

        var configDir = Path.Combine(config.VersionPath!, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        // isSave: false 让构造阶段的 Load 只准备内存对象；
        // 否则构造时就会先写一份没有 info 的默认配置（DefaultIgnoreCondition=WhenWritingNull
        // 会把 null 的 info 整个丢掉），主线程之外的读取方一旦抢在这中间读盘就会 NRE。
        var cfg = new ConfigEntity<VersionConfig>(configJsonPath, false) { Data = config };
        cfg.IsSave = true;
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

    /// <summary>
    /// 获取版本所在的数据根目录。PCLSE 的直接版本与 versions\extracted 下的解压版本
    /// 都必须使用已注册的数据根目录，不能从版本目录本身推导，否则更新时会在错误的位置安装。
    /// </summary>
    private static string GetVersionsRootPath(string gamePath, string? gameFolder = null)
    {
        var folder = FindGameFolder(gamePath);
        if (folder != null && !string.IsNullOrEmpty(folder.GameFolderPath) && IsPclSeFolder(folder))
            return Path.GetFullPath(folder.GameFolderPath);

        // 尚未注册（或直接调用）时，优先使用调用方给出的 PCLSE 数据根目录。
        if (!string.IsNullOrEmpty(gameFolder) && IsPclSeDataRoot(gameFolder))
            return Path.GetFullPath(gameFolder);

        return InferVersionsRootPath(gamePath);
    }

    private static string InferVersionsRootPath(string gamePath)
    {
        try
        {
            var current = new DirectoryInfo(Path.GetFullPath(gamePath));
            for (var depth = 0; depth < 4 && current != null; depth++)
            {
                if (IsVersionRootDirectoryName(current.Name))
                    return current.Parent?.FullName ?? current.FullName;
                current = current.Parent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"推断版本根目录失败（{gamePath}）：{ex.Message}");
        }

        return Path.GetFullPath(Path.Combine(gamePath, "..", ".."));
    }

    private static bool IsVersionRootDirectoryName(string? name) =>
        string.Equals(name, "versions", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "bedrock_versions", StringComparison.OrdinalIgnoreCase);

    private static bool IsPclSeFolder(GameFolderInfo? folder) =>
        folder != null &&
        (folder.GameFolderType == GameFolderType.PCLSE ||
         string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase));

    private static bool IsPclSeDataRoot(string gameFolder)
    {
        if (GetVersionRootFolderType(gameFolder) == GameFolderType.PCLSE) return true;

        try
        {
            var fullPath = Path.GetFullPath(gameFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return IsVersionRootDirectoryName(Path.GetFileName(fullPath)) ||
                   Directory.Exists(Path.Combine(fullPath, "versions")) ||
                   Directory.Exists(Path.Combine(fullPath, "bedrock_versions"));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断版本目录下是否存在 GDK 的 MicrosoftGame.Config。
    /// 解压后的 GDK 安装会把清单和它一起放在 inner 中，因此两者都要检查。
    /// </summary>
    private static bool HasGdkConfig(string gamePath, string manifestDirectory)
    {
        if (File.Exists(Path.Combine(gamePath, "MicrosoftGame.Config"))) return true;
        if (File.Exists(Path.Combine(manifestDirectory, "MicrosoftGame.Config"))) return true;

        var innerPath = Path.Combine(gamePath, "inner");
        return Directory.Exists(innerPath) &&
               File.Exists(Path.Combine(innerPath, "MicrosoftGame.Config"));
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

    private static string NormalizePath(string? path)
    {
        // 旧配置里的路径可能缺失，此时按空路径处理，避免整次扫描因单个坏条目抛异常。
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

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
