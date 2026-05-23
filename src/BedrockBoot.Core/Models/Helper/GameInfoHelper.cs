using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        var result = Directory.EnumerateDirectories(bedrockVersionsPath)
            .Select(GetVersionConfig)
            .Where(config => config?.Info != null && 
                             !string.IsNullOrEmpty(config.Info.VersionName) && 
                             !string.IsNullOrEmpty(config.Info.Version))
            .ToList();
        
        result.ForEach(config => Console.WriteLine($"Read {config.VersionPath}"));
        Console.WriteLine($"共获取到 {result.Count} 个实例");

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
        Console.WriteLine($"获取实例配置：{gamePath}");
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
        Console.WriteLine($"获取实例主文件：{gamePath}");
        // 仅搜索顶级目录，避免递归产生的性能消耗
        var exeFiles = Directory.EnumerateFiles(gamePath, "Minecraft*.exe")
                                .ToList();

        if (exeFiles.Count == 0) return string.Empty;

        // 这里的逻辑保持严谨：多个 EXE 可能意味着环境异常
        if (exeFiles.Count > 1)
        {
            throw new InvalidOperationException(
                $"检测到异常：目录中存在多个 Minecraft EXE 文件 ({exeFiles.Count}个)。\n" +
                $"请清理目录以防潜在风险。\n路径：{gamePath}");
        }

        Console.WriteLine($"已获取主文件：{exeFiles[0]}");
        
        return Path.GetFileName(exeFiles[0]);
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
        Console.WriteLine($"保存版本配置：{config.VersionPath}");
        if (config == null) return;

        var configDir = Path.Combine(config.VersionPath, ConfigSubPath);
        var configJsonPath = Path.Combine(configDir, ConfigFileName);

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        var cfg = new ConfigEntity<VersionConfig>(configJsonPath) { Data = config };
        cfg.Save();
    }
}