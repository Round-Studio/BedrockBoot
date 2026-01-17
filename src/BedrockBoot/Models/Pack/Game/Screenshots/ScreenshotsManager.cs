using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Screenshots;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Screenshots;

public class ScreenshotsManager
{
    public VersionConfig VersionConfig { get; set; }

    public ScreenshotsManager(VersionConfig versionInfo)
    {
        VersionConfig = versionInfo;
    }

    public Dictionary<string, List<ScreenshotsInfo>> GetScreenshots()
    {
        var result = new Dictionary<string, List<ScreenshotsInfo>>();
        var users = GetInstanceScreenshotsPath();

        foreach (var user in users)
        {
            var files = Directory.GetFiles(user.Value, "*.jpeg", SearchOption.AllDirectories)
                                            .ToList();
            var resultInfos = new List<ScreenshotsInfo>();
            
            files.ForEach(file =>
            {
                var confFile = file.Replace(".jpeg", ".json");
                var conf = new ConfigEntity<ScreenshotsInfo>(confFile,false);
                conf.Data.FilePath = file;
                resultInfos.Add(conf.Data);
            });
            
            result.Add(user.Key, resultInfos);
        }
        
        return result;
    }

    private Dictionary<string, string> GetInstanceScreenshotsPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            result.Add("Shared", Path.Combine(
                IsolationCore.GetRealPath(VersionConfig),
                @"LocalState\games\com.mojang\Screenshots"
            ));
        }
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            var dir = Path.Combine(
                IsolationCore.GetRealPath(VersionConfig),
                "Users"
            );

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var users = Directory.GetDirectories(dir).ToList();
            users.ForEach(user =>
            {
                var path = Path.Combine(user, "games", "com.mojang", "Screenshots");
                if (Path.Exists(path))
                    result.Add(Path.GetFileName(user), path);
                else
                {
                    Directory.CreateDirectory(path);
                    result.Add(Path.GetFileName(user), path);
                }
            });
        }

        return result
            .Where(path => Directory.Exists(path.Value))
            .ToDictionary(path => path.Key, path => path.Value);
    }
}