using System.IO;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Game;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Helper;

public class GameInfoHelper
{
    public static VersionInfo GetVersionInfo(string gamePath)
    {
        var jsonFile = Path.Combine(gamePath,"version.json");
        if (!File.Exists(jsonFile))
            return null;

        var json = File.ReadAllText(jsonFile);
        return JsonSerializer.Deserialize<VersionInfo>(json);
    }

    public static GameVersionType GetGameVersionType(string typeStr)
    {
        switch (typeStr)
        {
            case "Release":
                return GameVersionType.Release;
            case "Preview":
                return GameVersionType.Preview;
            case "Beta":
                return GameVersionType.Beta;
            default:
                return GameVersionType.Release;
        }
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
                BuildType = GameBuildType.Uwp, // 旧版 BedrockBoot 也只能安装 UWP 版本，所以这个鬼地方写死就行了 orz...
                VersionType = GetGameVersionType(oldBedrockBootConfig.Data.Type)
            };
            
            bodyConfig.Save();
        }
        else
        {
            bodyConfig = new ConfigEntity<VersionConfig>(bedrockBootJson);
            bodyConfig.Load();
        }

        bodyConfig.Data.VersionPath = gamePath;

        return bodyConfig.Data;
    }
}