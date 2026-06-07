using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Core.Models.Helper;

public static class GameInfoHelper
{
    private const string ConfigSubPath = "config/BedrockBoot2";
    private const string ConfigFileName = "config.json";
    private const string IndexFileName = "index.json";

    /// <summary>
    /// 获取版本配置列表
    /// </summary>
    public static List<VersionConfig> GetVersionConfigs(string gameFolder)
    {
        var bedrockVersionsPath = Path.Combine(gameFolder, "bedrock_versions");

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

    /// <summary>
    /// 获取单个版本的详细配置
    /// </summary>
    public static VersionConfig GetVersionConfig(string gamePath)
    {
        var configDir = Path.Combine(gamePath, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        ConfigEntity<VersionConfig> configEntity;

        // 检查配置文件是否存在
        if (!File.Exists(configJsonPath))
        {
            var manifestPath = Path.Combine(gamePath, "appxmanifest.xml");
            if (!File.Exists(manifestPath)) return null;

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
        if (string.IsNullOrEmpty(bodyFile)) return null;

        var data = configEntity.Data;
        data.VersionPath = gamePath;
        data.BodyFile = bodyFile;

        if (data.Config.IsVersionIsolated && OperatingSystem.IsLinux())
        {
            data.Config.IsVersionIsolated = false;
            SaveVersionConfig(data);
        }

        return data;
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

    /// <summary>
    /// 验证版本有效性
    /// </summary>
    public static bool IsInvalidVersion(VersionConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.VersionPath)) return false;

        var indexJson = Path.Combine(config.VersionPath, ConfigSubPath, IndexFileName);

        if (!File.Exists(indexJson)) return false;

        try
        {
            // 简单的内容检查，避免加载大文件
            var content = File.ReadAllText(indexJson);
            if (string.IsNullOrWhiteSpace(content) || content == "[]") return false;

            var body = new ConfigEntity<List<GameFileInfo>>(indexJson);
            body.Load();
            return body.Data != null && body.Data.Count > 0;
        }
        catch
        {
            return false;
        }
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
}
