using System;
using System.IO;
using BedrockBoot.Core.Global;
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