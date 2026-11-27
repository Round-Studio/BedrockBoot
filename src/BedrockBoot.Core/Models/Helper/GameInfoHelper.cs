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
using System.Text;
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

        // 枚举目录时容错：单个目录不可访问或在扫描期间被删除，不应让整轮扫描失败；
        // 同时跳过指向远程位置的重解析点，避免探测时触发 SMB 访问。
        var dirs = EnumerateLocalDirectories(bedrockVersionsPath);
        if (isPclSe)
        {
            var extractedPath = dirs.FirstOrDefault(dir =>
                string.Equals(Path.GetFileName(dir), "extracted", StringComparison.OrdinalIgnoreCase));
            if (extractedPath != null)
            {
                dirs.Remove(extractedPath);
                dirs.AddRange(EnumerateLocalDirectories(extractedPath));
            }
        }

        // 注册的目录本身可能就是单个版本（例如 PCLSE 配置直接指向 extracted\<版本>）。
        if (string.IsNullOrEmpty(rootName) && LooksLikeGameInstance(bedrockVersionsPath))
            dirs.Insert(0, bedrockVersionsPath);

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
        // inner 可能是指向远程共享的联接点，探测前先拒绝。
        var manifestPath = Path.Combine(gamePath, "appxmanifest.xml");
        if (!File.Exists(manifestPath))
        {
            var innerDirectory = Path.Combine(gamePath, "inner");
            if (!IsRemoteLinkedPath(innerDirectory))
            {
                var innerManifestPath = Path.Combine(innerDirectory, "appxmanifest.xml");
                if (File.Exists(innerManifestPath)) manifestPath = innerManifestPath;
            }
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
            if (!IsRemoteLinkedPath(innerPath) && Directory.Exists(innerPath))
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

    /// <summary>
    /// 获取用于启动/注册的游戏包目录。
    /// PCLSE 解压版本可能把清单（appxmanifest.xml / MicrosoftGame.Config）放在 inner 下，
    /// 外层版本目录里没有清单，UWP 注册会失败，因此需要指向真正包含清单的目录。
    /// 版本数据本身仍使用外层 VersionPath。
    /// </summary>
    public static string GetGamePackageDirectory(string? versionPath)
    {
        if (string.IsNullOrEmpty(versionPath)) return string.Empty;

        try
        {
            if (HasPackageManifest(versionPath)) return versionPath;

            var innerPath = Path.Combine(versionPath, "inner");
            if (Directory.Exists(innerPath) && !IsRemoteLinkedPath(innerPath) && HasPackageManifest(innerPath))
                return innerPath;

            return versionPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取游戏包目录失败（{versionPath}）：{ex.Message}");
            return versionPath;
        }
    }

    /// <summary>
    /// 获取用于启动/注册的游戏包目录。
    /// </summary>
    public static string GetGamePackageDirectory(VersionConfig? config) =>
        config == null ? string.Empty : GetGamePackageDirectory(config.VersionPath);

    private static bool HasPackageManifest(string path) =>
        File.Exists(Path.Combine(path, "appxmanifest.xml")) ||
        File.Exists(Path.Combine(path, "MicrosoftGame.Config"));

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
        var fullPath = Path.GetFullPath(gamePath);
        var parent = Path.GetDirectoryName(fullPath);

        // PCLSE 解压实例位于 <versions>\extracted\<名字>：下载器会用
        // GetGameFolderRootName(VersionsRootPath) + 目录名 定位安装位置，
        // 因此这里必须返回 extracted 目录本身，更新才会装回所选实例。
        if (!string.IsNullOrEmpty(parent) &&
            string.Equals(Path.GetFileName(parent), "extracted", StringComparison.OrdinalIgnoreCase) &&
            (GetVersionRootFolderType(parent) == GameFolderType.PCLSE ||
             (!string.IsNullOrEmpty(gameFolder) && IsPclSeDataRoot(gameFolder))))
            return parent;

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

    /// <summary>
    /// 枚举子目录：跳过指向远程位置（UNC、网络映射盘）的重解析点，并吞掉枚举异常，
    /// 避免单个坏目录让整轮版本扫描失败。
    /// </summary>
    private static List<string> EnumerateLocalDirectories(string path)
    {
        var result = new List<string>();
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                if (IsRemoteLinkedPath(directory))
                {
                    Console.WriteLine($@"忽略指向远程位置的目录：{directory}");
                    continue;
                }

                result.Add(directory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"枚举目录失败（{path}）：{ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 判断路径是否是指向远程位置的重解析点（符号链接/联接点）。
    /// 只读取本地重解析数据，不访问目标。
    /// </summary>
    private static bool IsRemoteLinkedPath(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) == 0) return false;

            var target = Directory.ResolveLinkTarget(path, false) ?? File.ResolveLinkTarget(path, false);
            if (target == null) return false;

            var root = Path.GetPathRoot(target.FullName);
            return string.IsNullOrEmpty(root) || IsRemoteRoot(root);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRemoteRoot(string root) =>
        root.StartsWith("\\\\", StringComparison.Ordinal) ||
        root.StartsWith("//", StringComparison.Ordinal) ||
        root.StartsWith(@"\\?\", StringComparison.Ordinal) ||
        root.StartsWith(@"\??\", StringComparison.Ordinal) ||
        IsMappedNetworkDrive(root);

    /// <summary>
    /// 判断驱动器根目录是否为网络映射盘（例如 net use 的 Z:\）。
    /// 探测这类路径会向共享发送 SMB 认证。
    /// </summary>
    private static bool IsMappedNetworkDrive(string root)
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            var driveRoot = Path.GetPathRoot(root);
            if (string.IsNullOrEmpty(driveRoot) || driveRoot.Length < 2 || driveRoot[1] != ':') return false;
            return new DriveInfo(driveRoot).DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断目录是否像一个游戏实例（清单、GDK 配置或可执行文件）。
    /// </summary>
    private static bool LooksLikeGameInstance(string path)
    {
        try
        {
            if (File.Exists(Path.Combine(path, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(path, "MicrosoftGame.Config"))) return true;
            if (Directory.EnumerateFiles(path, "Minecraft*.exe").Any()) return true;

            var innerPath = Path.Combine(path, "inner");
            if (IsRemoteLinkedPath(innerPath) || !Directory.Exists(innerPath)) return false;

            if (File.Exists(Path.Combine(innerPath, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(innerPath, "MicrosoftGame.Config"))) return true;
            return Directory.EnumerateFiles(innerPath, "Minecraft*.exe").Any();
        }
        catch
        {
            return false;
        }
    }

    private static bool HasVersionInstances(string versionsRoot)
    {
        try
        {
            if (!Directory.Exists(versionsRoot)) return false;
            if (EnumerateLocalDirectories(versionsRoot).Any(LooksLikeGameInstance)) return true;

            var extractedPath = Path.Combine(versionsRoot, "extracted");
            return Directory.Exists(extractedPath) &&
                   EnumerateLocalDirectories(extractedPath).Any(LooksLikeGameInstance);
        }
        catch
        {
            return false;
        }
    }

    private static string GetPclSeRootName(string gameFolder)
    {
        var fullPath = Path.GetFullPath(gameFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(fullPath);
        if (string.Equals(leaf, "versions", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(leaf, "bedrock_versions", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(leaf, "extracted", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        // versions 与 bedrock_versions 可能同时存在：优先选择真正包含实例的布局，
        // 否则会扫到空目录，另一套布局里的实例就“消失”了。
        var rootNames = new[] { "versions", "bedrock_versions" };
        foreach (var rootName in rootNames)
            if (HasVersionInstances(Path.Combine(fullPath, rootName)))
                return rootName;

        foreach (var rootName in rootNames)
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

/// <summary>
/// PCLSE 目录导入的辅助逻辑：受限的 YAML 读取、路径校验与版本布局探测。
/// 与界面无关，便于独立测试。
/// </summary>
public static class PclSeHelper
{
    // 配置文件与解析结果都设上限：PCLSE 配置由外部程序写入，不能假定其体积和条目数量可控。
    private const long MaxConfigFileBytes = 512 * 1024;
    private const int MaxConfigLines = 4096;
    private const int MaxConfigLineLength = 2048;
    private const int MaxConfigEntries = 256;
    private const int MaxConfigValueLength = 1024;
    private const int MaxDirectoryScan = 256;
    private const int MaxReparsePointHops = 32;

    private static readonly string[] ConfigKeys = { "BedrockFolder", "BedrockExternalFolders" };

    public static bool IsPclSeRegistration(GameFolderInfo? folder) =>
        folder != null &&
        (folder.GameFolderType == GameFolderType.PCLSE ||
         string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 从启动器目录逐级向上查找包含 PCL\config.v1.yml 的目录。
    /// </summary>
    public static IEnumerable<string> GetRoots(string launcherRoot)
    {
        if (!TryNormalizeLocalPath(launcherRoot, null, out var current)) yield break;

        for (var i = 0; i < 5 && !string.IsNullOrWhiteSpace(current); i++)
        {
            if (LocalFileExists(Path.Combine(current, "PCL", "config.v1.yml"))) yield return current;

            string? parent;
            try
            {
                parent = Directory.GetParent(current)?.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找 PCLSE 上级目录失败（{current}）：{ex.Message}");
                yield break;
            }

            if (string.IsNullOrEmpty(parent) ||
                string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                break;

            current = parent;
        }
    }

    /// <summary>
    /// 把 PCLSE 配置里的目录适配成启动器使用的数据根目录。
    /// </summary>
    public static string? PrepareFolder(string source)
    {
        try
        {
            if (!TryNormalizeLocalPath(source, null, out var normalized) || !Directory.Exists(normalized))
                return null;

            var leafName = Path.GetFileName(normalized);
            if (leafName.Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                leafName.Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(normalized);

            if (LocalDirectoryExists(Path.Combine(normalized, "versions")) ||
                LocalDirectoryExists(Path.Combine(normalized, "bedrock_versions")))
                return normalized;

            if (leafName.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(normalized) is { } versionsDirectory &&
                (Path.GetFileName(versionsDirectory).Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFileName(versionsDirectory).Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase)))
                return Path.GetDirectoryName(versionsDirectory);

            // 配置可能直接指向某个版本目录（例如 ...\extracted\<版本>）。
            // 这时要注册它所属的数据根目录，否则会把版本目录当成数据根、扫描不到任何实例。
            if (LooksLikeGameInstance(normalized))
            {
                var parent = Path.GetDirectoryName(normalized);
                if (parent != null &&
                    Path.GetFileName(parent).Equals("extracted", StringComparison.OrdinalIgnoreCase))
                    parent = Path.GetDirectoryName(parent);

                if (parent != null &&
                    (Path.GetFileName(parent).Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetFileName(parent).Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase)))
                    return Path.GetDirectoryName(parent);

                return normalized;
            }

            return HasVersionDirectories(normalized) ? normalized : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"适配 PCLSE 目录失败（{source}）：{ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 判断已注册目录是否真的包含 PCLSE 的版本布局或可发现的游戏实例。
    /// 只要目录存在就认为导入成功会把空目录也算进来，因此必须额外校验内容。
    /// </summary>
    public static bool IsUsableFolder(string folder)
    {
        try
        {
            if (!TryNormalizeLocalPath(folder, null, out var normalized) || !Directory.Exists(normalized))
                return false;

            if (LooksLikeGameInstance(normalized)) return true;

            return ContainsGameInstance(normalized) || HasVersionLayout(normalized);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查 PCLSE 目录失败（{folder}）：{ex.Message}");
            return false;
        }
    }

    public static List<string> ReadDirectories(string launcherRoot)
    {
        var result = new List<string>();
        if (!TryNormalizeLocalPath(Path.Combine(launcherRoot, "PCL", "config.v1.yml"), null, out var configPath) ||
            !File.Exists(configPath))
            return result;

        var values = ReadConfig(configPath);

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!TryNormalizeLocalPath(path, launcherRoot, out var fullPath))
            {
                Console.WriteLine($"忽略远程或无效的 PCLSE 基岩目录：{path}");
                return;
            }

            if (!result.Any(existing => PathsEqual(existing, fullPath))) result.Add(fullPath);
        }

        if (values.TryGetValue("BedrockFolder", out var folders) && folders.Count > 0)
            Add(folders[0]);
        else
        {
            // PCLSE 现在默认使用 .minecraft\\bedrock，旧版本则使用
            // .minecraft\\bedrock_versions 或启动器根目录下的 bedrock_versions。
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock"));
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock_versions"));
            Add(Path.Combine(launcherRoot, "bedrock_versions"));
        }

        // BedrockExternalFolders 既可能写成 YAML 序列，也可能写成以分号分隔的标量。
        if (values.TryGetValue("BedrockExternalFolders", out var externalFolders))
            foreach (var externalValue in externalFolders)
                foreach (var external in externalValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    Add(external.Trim());

        return result;
    }

    /// <summary>
    /// 只解析 PCLSE 的基岩目录相关键，并限制读取的行数、条目数与单个值的长度。
    /// </summary>
    private static Dictionary<string, List<string>> ReadConfig(string path)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in ConfigKeys) result[key] = new List<string>();

        string? sequenceKey = null;
        foreach (var line in ReadBoundedLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            // YAML 序列项归属到上一个取值为空的键（例如 BedrockExternalFolders）。
            if (trimmed == "-") continue;
            if (trimmed.Length > 1 && trimmed[0] == '-' && char.IsWhiteSpace(trimmed[1]))
            {
                if (sequenceKey != null)
                    AddBoundedValue(result[sequenceKey], ParseValue(StripComment(trimmed[1..].Trim())));
                continue;
            }

            var separator = trimmed.IndexOf(':');
            if (separator <= 0)
            {
                sequenceKey = null;
                continue;
            }

            var key = trimmed[..separator].Trim();
            if (!result.TryGetValue(key, out var values))
            {
                // 不需要的键直接跳过，避免把整份配置读进内存。
                sequenceKey = null;
                continue;
            }

            var value = StripComment(trimmed[(separator + 1)..].Trim());
            sequenceKey = value.Length == 0 ? key : null;

            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                foreach (var item in SplitFlowSequence(value[1..^1]))
                    AddBoundedValue(values, ParseValue(item));
            }
            else if (value.Length > 0)
            {
                AddBoundedValue(values, ParseValue(value));
            }
        }

        return result;
    }

    private static IEnumerable<string> ReadBoundedLines(string path)
    {
        long length;
        try
        {
            length = new FileInfo(path).Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取 PCLSE 配置失败（{path}）：{ex.Message}");
            yield break;
        }

        if (length > MaxConfigFileBytes)
        {
            Console.WriteLine($"忽略过大的 PCLSE 配置文件（{length} 字节）：{path}");
            yield break;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);

        for (var i = 0; i < MaxConfigLines; i++)
        {
            var line = reader.ReadLine();
            if (line == null) yield break;
            if (line.Length > MaxConfigLineLength) continue;
            yield return line;
        }
    }

    private static void AddBoundedValue(List<string> values, string value)
    {
        if (values.Count >= MaxConfigEntries) return;

        var trimmed = value.Trim();
        if (trimmed.Length == 0) return;
        if (trimmed.Length > MaxConfigValueLength) trimmed = trimmed[..MaxConfigValueLength];
        values.Add(trimmed);
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"");
        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
            return value[1..^1].Replace("''", "'");
        return value;
    }

    private static string StripComment(string value)
    {
        if (value.Length == 0) return value;

        char quote = '\0';
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                quote = ch;
                continue;
            }

            if (ch == '#' && (i == 0 || char.IsWhiteSpace(value[i - 1])))
                return value[..i].TrimEnd();
        }

        return value;
    }

    private static IEnumerable<string> SplitFlowSequence(string value)
    {
        var items = new List<string>();
        if (value.Length == 0) return items;

        var buffer = new StringBuilder();
        char quote = '\0';
        foreach (var ch in value)
        {
            if (quote != '\0')
            {
                buffer.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                quote = ch;
                buffer.Append(ch);
                continue;
            }

            if (ch == ',')
            {
                if (buffer.Length > 0) items.Add(buffer.ToString().Trim());
                buffer.Clear();
                continue;
            }

            buffer.Append(ch);
        }

        if (buffer.Length > 0) items.Add(buffer.ToString().Trim());
        return items;
    }

    private static bool ContainsGameInstance(string folder, int depth = 0)
    {
        if (depth > 2) return false;

        foreach (var directory in EnumerateLocalDirectories(folder))
        {
            var name = Path.GetFileName(directory);
            if (name.Equals("cache", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("loaders", StringComparison.OrdinalIgnoreCase))
                continue;

            if (name.Equals("extracted", StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsGameInstance(directory, depth + 1)) return true;
                continue;
            }

            if (LooksLikeGameInstance(directory)) return true;
        }

        return false;
    }

    /// <summary>
    /// 枚举子目录时先解析链接：子目录也可能是指向远程共享的联接点，
    /// 直接探测会触发 SMB 访问并泄露凭据。
    /// </summary>
    private static List<string> EnumerateLocalDirectories(string folder)
    {
        string[] directories;
        try
        {
            directories = Directory.EnumerateDirectories(folder).Take(MaxDirectoryScan).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"枚举目录失败（{folder}）：{ex.Message}");
            return new List<string>();
        }

        var result = new List<string>(directories.Length);
        foreach (var directory in directories)
            if (TryNormalizeLocalPath(directory, null, out var normalized))
                result.Add(normalized);

        return result;
    }

    private static bool LooksLikeGameInstance(string folder)
    {
        try
        {
            if (File.Exists(Path.Combine(folder, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(folder, "MicrosoftGame.Config"))) return true;
            if (Directory.EnumerateFiles(folder, "Minecraft*.exe").Any()) return true;

            var inner = Path.Combine(folder, "inner");
            if (!TryNormalizeLocalPath(inner, null, out var innerPath) || !Directory.Exists(innerPath))
                return false;

            if (File.Exists(Path.Combine(innerPath, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(innerPath, "MicrosoftGame.Config"))) return true;
            return Directory.EnumerateFiles(innerPath, "Minecraft*.exe").Any();
        }
        catch
        {
            return false;
        }
    }

    private static bool HasVersionLayout(string folder)
    {
        foreach (var rootName in new[] { "versions", "bedrock_versions" })
        {
            var versionsPath = Path.Combine(folder, rootName);
            if (!TryNormalizeLocalPath(versionsPath, null, out var versionsRoot) ||
                !Directory.Exists(versionsRoot))
                continue;

            if (HasRealVersionDirectories(versionsRoot)) return true;

            var extractedPath = Path.Combine(versionsRoot, "extracted");
            if (TryNormalizeLocalPath(extractedPath, null, out var extractedRoot) &&
                Directory.Exists(extractedRoot) && HasRealVersionDirectories(extractedRoot))
                return true;
        }

        return false;
    }

    private static bool HasRealVersionDirectories(string path)
    {
        // 只有子目录里真的存在游戏文件/清单，才算可识别的版本；
        // 否则随便一个空目录都能通过导入校验。
        return EnumerateLocalDirectories(path).Any(dir =>
        {
            var name = Path.GetFileName(dir);
            return !name.Equals("cache", StringComparison.OrdinalIgnoreCase) &&
                   !name.Equals("loaders", StringComparison.OrdinalIgnoreCase) &&
                   !name.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                   LooksLikeGameInstance(dir);
        });
    }

    private static bool HasVersionDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).Any(dir =>
            {
                var name = Path.GetFileName(dir);
                return !name.Equals("cache", StringComparison.OrdinalIgnoreCase) &&
                       !name.Equals("loaders", StringComparison.OrdinalIgnoreCase);
            });
        }
        catch
        {
            return false;
        }
    }

    private static bool LocalFileExists(string path) =>
        TryNormalizeLocalPath(path, null, out var normalized) && File.Exists(normalized);

    private static bool LocalDirectoryExists(string path) =>
        TryNormalizeLocalPath(path, null, out var normalized) && Directory.Exists(normalized);

    /// <summary>
    /// 把路径规范化为真实的本地路径。解析符号链接和联接点后，
    /// 目标为远程位置（UNC 共享等）的路径会被拒绝，后续探测才不会触发 SMB 访问。
    /// </summary>
    public static bool TryNormalizeLocalPath(string? path, string? basePath, out string normalized)
    {
        normalized = string.Empty;
        var value = path?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.StartsWith("\\\\") || value.StartsWith("//") ||
            value.StartsWith(@"\\?\") || value.Contains("://", StringComparison.Ordinal))
            return false;

        try
        {
            var fullPath = string.IsNullOrEmpty(basePath)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(value, basePath);

            if (!TryResolveLocalTarget(fullPath, out var resolved)) return false;

            var root = Path.GetPathRoot(resolved);
            if (string.IsNullOrEmpty(root) || IsRemoteRoot(root)) return false;

            normalized = string.Equals(resolved, root, StringComparison.OrdinalIgnoreCase)
                ? resolved
                : resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }
        catch
        {
            normalized = string.Empty;
            return false;
        }
    }

    public static bool PathsEqual(string? left, string? right)
    {
        if (!TryNormalizeLocalPath(left, null, out var normalizedLeft) ||
            !TryNormalizeLocalPath(right, null, out var normalizedRight))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 逐级解析符号链接与联接点，返回最终的真实本地路径。
    /// </summary>
    private static bool TryResolveLocalTarget(string fullPath, out string resolved)
    {
        resolved = fullPath;

        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root) || IsRemoteRoot(root)) return false;

        var current = root;
        var pending = new Queue<string>(SplitSegments(fullPath[root.Length..]));
        var hops = 0;

        while (pending.Count > 0)
        {
            var segment = pending.Dequeue();
            var next = Path.Combine(current, segment);

            if (!TryGetAttributes(next, out var attributes))
            {
                // 不存在的路径不可能包含链接，直接拼出剩余部分。
                current = pending.Count == 0
                    ? next
                    : Path.Combine(next, string.Join(Path.DirectorySeparatorChar, pending));
                pending.Clear();
                break;
            }

            if ((attributes & FileAttributes.ReparsePoint) == 0)
            {
                current = next;
                continue;
            }

            if (++hops > MaxReparsePointHops) return false;

            // 非链接的重解析点（例如 OneDrive 占位文件）解析不出目标，按普通路径处理。
            if (!TryResolveImmediateLink(next, out var target))
            {
                current = next;
                continue;
            }

            var targetRoot = Path.GetPathRoot(target);
            if (string.IsNullOrEmpty(targetRoot) || IsRemoteRoot(targetRoot)) return false;

            // 目标自身可能仍是链接：把它拆成路径段重新入队，继续逐级解析。
            // 只解析直接目标，因为 returnFinalTarget 会跟随整条链接链并访问远程目标。
            var remaining = pending.ToArray();
            pending.Clear();
            foreach (var item in SplitSegments(target[targetRoot.Length..])) pending.Enqueue(item);
            foreach (var item in remaining) pending.Enqueue(item);

            current = targetRoot;
        }

        resolved = current;
        return true;
    }

    private static string[] SplitSegments(string path) =>
        path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// 解析链接的<b>直接</b>目标。注意不能使用 returnFinalTarget：
    /// 它会沿链接链继续解析，访问远程目标时实测会阻塞数十秒并触发 SMB 连接。
    /// </summary>
    private static bool TryResolveImmediateLink(string path, out string target)
    {
        target = string.Empty;

        var resolved = ResolveDirectoryLink(path) ?? ResolveFileLink(path);
        if (string.IsNullOrEmpty(resolved)) return false;

        // 目标可能带 \\?\ 前缀，去掉后才能与常规路径比较。
        if (resolved.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            target = resolved;
            return true;
        }

        if (resolved.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            resolved.StartsWith(@"\??\", StringComparison.Ordinal))
            resolved = resolved[4..];

        target = resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(resolved, Path.GetPathRoot(resolved), StringComparison.OrdinalIgnoreCase))
            target = resolved;
        return target.Length > 0;
    }

    private static string? ResolveDirectoryLink(string path)
    {
        try
        {
            return Directory.ResolveLinkTarget(path, false)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveFileLink(string path)
    {
        try
        {
            return File.ResolveLinkTarget(path, false)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch
        {
            attributes = default;
            return false;
        }
    }

    private static bool IsRemoteRoot(string root) =>
        root.StartsWith("\\\\", StringComparison.Ordinal) ||
        root.StartsWith("//", StringComparison.Ordinal) ||
        root.StartsWith(@"\\?\", StringComparison.Ordinal) ||
        root.StartsWith(@"\??\", StringComparison.Ordinal) ||
        IsMappedNetworkDrive(root);

    /// <summary>
    /// 判断驱动器根目录是否为网络映射盘（例如 net use 的 Z:\）。
    /// 这类路径的根既不是 UNC 也不是设备路径，但探测时同样会向共享发送 SMB 认证。
    /// </summary>
    private static bool IsMappedNetworkDrive(string root)
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            var driveRoot = Path.GetPathRoot(root);
            if (string.IsNullOrEmpty(driveRoot) || driveRoot.Length < 2 || driveRoot[1] != ':')
                return false;

            return new DriveInfo(driveRoot).DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }
}
