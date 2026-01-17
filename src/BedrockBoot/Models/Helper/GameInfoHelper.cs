using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Helper;

public class GameInfoHelper
{
    public static VersionInfo GetVersionInfo(string gamePath)
    {
        var jsonFile = Path.Combine(gamePath, "version.json");
        if (!File.Exists(jsonFile))
            return null;

        var json = File.ReadAllText(jsonFile);
        return JsonSerializer.Deserialize<VersionInfo>(json);
    }

    public static MinecraftGameTypeVersion GetGameVersionType(string typeStr)
    {
        switch (typeStr)
        {
            case "Release":
                return MinecraftGameTypeVersion.Release;
            case "Preview":
                return MinecraftGameTypeVersion.Preview;
            case "Beta":
                return MinecraftGameTypeVersion.Beta;
            default:
                return MinecraftGameTypeVersion.Release;
        }
    }

    public static List<VersionConfig> GetVersionConfigs(string gameFolder)
    {
        var result = new List<VersionConfig>();
        var versions = Directory.GetDirectories(Path.Combine(gameFolder, "bedrock_versions")).ToList();

        versions.ForEach(x =>
        {
            var body = GetVersionConfig(x);
            if (body != null &&
                !string.IsNullOrEmpty(body.Info.VersionName) &&
                !string.IsNullOrEmpty(body.Info.Version))
            {
                result.Add(body);
            }
        });
        return result;
    }

    public static VersionConfig GetVersionConfig(string gamePath)
    {
        var bedrockBootJson = Path.Combine(gamePath, "config", "BedrockBoot2", "config.json");
        ConfigEntity<VersionConfig> bodyConfig = null;

        if (!File.Exists(bedrockBootJson)) // 没有 BedrockBoot 2 的配置文件时
        {
            Directory.CreateDirectory(Path.Combine(gamePath, "config", "BedrockBoot2"));
            bodyConfig = new ConfigEntity<VersionConfig>(bedrockBootJson);
            bodyConfig.Load();

            var oldBedrockBootConfig = new ConfigEntity<VersionInfo>(Path.Combine(gamePath, "version.json"));
            oldBedrockBootConfig.Load();

            bodyConfig.Data.Info = new VersionConfig.VersionInfo()
            {
                Version = oldBedrockBootConfig.Data.RealVersion,
                VersionName = oldBedrockBootConfig.Data.VersionName,
                BuildType = MinecraftBuildTypeVersion.UWP, // 旧版 BedrockBoot 也只能安装 UWP 版本，所以这个鬼地方写死就行了 orz...
                VersionType = GetGameVersionType(oldBedrockBootConfig.Data.Type)
            };

            bodyConfig.Save();
        }
        else
        {
            bodyConfig = new ConfigEntity<VersionConfig>(bedrockBootJson);
            bodyConfig.Load();
        }

        var bodyFile = GetBodyFile(gamePath);

        if (string.IsNullOrEmpty(bodyFile))
            return null;
        
        bodyConfig.Data.VersionPath = gamePath;
        bodyConfig.Data.BodyFile = bodyFile;

        return bodyConfig.Data;
    }

    public static string GetBodyFile(string gamePath)
    {
        var files = Directory.GetFiles(gamePath, "*.exe")
            .Where(x => Path.GetFileName(x).StartsWith("Minecraft"))
            .ToList();

        if (files.Count() <= 0)
            return string.Empty;
        if (files.Count() > 1)
            throw new FileNotFoundException(
                $"无法找到对应的 EXE 文件，原因是该目录中有 {files.Count()} 个 EXE，有很大概率是蠕虫病毒的感染，请尝试查杀病毒或删除对应文件以解决该问题。\nFiles:\n{string.Join('\n', files)}");
        
        Console.WriteLine($@"目标实例本体文件：{files[0]}");
        
        return Path.GetFileName(files[0]);
    }

    public static bool IsInvalidVersion(VersionConfig config)
    {
        var indexJson = Path.Combine(config.VersionPath, "config", "BedrockBoot2", "index.json");
        
        if (!File.Exists(indexJson))
            return false;
        if (string.IsNullOrEmpty(File.ReadAllText(indexJson)))
            return false;

        var body = new ConfigEntity<List<GameFileInfo>>(indexJson);
        body.Load();

        if (body.Data.Count <= 0)
            return false;
        
        return true;
    }

    public static void SaveVersionConfig(VersionConfig config)
    {
        var bedrockBootJson = Path.Combine(config.VersionPath, "config", "BedrockBoot2", "config.json");

        if (!Directory.Exists(Path.Combine(config.VersionPath, "config", "BedrockBoot2")))
            Directory.CreateDirectory(Path.Combine(config.VersionPath, "config", "BedrockBoot2"));

        var cfg = new ConfigEntity<VersionConfig>(bedrockBootJson);
        cfg.Data = config;
        cfg.Save();
    }
}