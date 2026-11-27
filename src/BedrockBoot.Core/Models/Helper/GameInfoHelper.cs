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
        var bedrockVersionsPath = type == GameFolderType.BedrockBoot
            ? "bedrock_versions"
            : "versions";

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

        // 使用 EnumerateDirectories 提高大目录下的性能
        // 物化目录列表：保留原始顺序作为索引
        var dirs = Directory.EnumerateDirectories(bedrockVersionsPath).ToList();

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
            var rebuilt = TryCreateVersionInfo(gamePath);
            if (rebuilt == null) return null;

            data.Info = rebuilt;
            needSave = true;
        }

        // 绑定运行时路径
        var bodyFile = GetBodyFile(gamePath);
        if (string.IsNullOrEmpty(bodyFile) && !isDebug) return null;

        data.VersionPath = gamePath;
        data.VersionsRootPath = Path.GetFullPath(Path.Combine(gamePath, @"..\.."));
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
    /// UWP 走 appxmanifest.xml；GDK 包没有清单，退化用文件夹名当版本号（与下载器写入的规则一致）。
    /// 任何读取/解析异常都返回 null —— 解压过程中清单可能只写了一半，不能把异常往上抛，
    /// 更不能留下残缺配置。
    /// </summary>
    private static VersionConfig.VersionInfo? TryCreateVersionInfo(string gamePath)
    {
        var isGdk = File.Exists(Path.Combine(gamePath, "MicrosoftGame.Config"));
        var manifestPath = Path.Combine(gamePath, "appxmanifest.xml");

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
    /// 寻找 Minecraft 执行文件
    /// </summary>
    public static string GetBodyFile(string gamePath)
    {
        // 仅搜索顶级目录，避免递归产生的性能消耗
        // 用 FirstOrDefault 提前终止，无需物化为 List
        var exeFile = Directory.EnumerateFiles(gamePath, "Minecraft*.exe").FirstOrDefault();

        if (exeFile == null) return string.Empty;

        return Path.GetFileName(exeFile);
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
        if (config == null || string.IsNullOrEmpty(config.VersionPath)) return;

        var configDir = Path.Combine(config.VersionPath, ConfigSubPath);
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

        var gameFolder = GlobalModel.Config.Data.GameFolders
            .Find(x => config.VersionPath.StartsWith(x.GameFolderPath, StringComparison.OrdinalIgnoreCase));

        return gameFolder?.GameFolderType ?? GameFolderType.Other;
    }
}