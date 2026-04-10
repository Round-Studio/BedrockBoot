using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Screenshots;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Screenshots;

public class ScreenshotsManager
{
    public ScreenshotsManager(VersionConfig versionInfo)
    {
        VersionConfig = versionInfo;
    }

    public VersionConfig VersionConfig { get; set; }

    public Dictionary<string, List<ScreenshotsInfo>> GetScreenshots()
    {
        var result = new Dictionary<string, List<ScreenshotsInfo>>();
        var users = GetInstanceScreenshotsPath();

        foreach (var user in users)
        {
            if(!Directory.Exists(user.Value)) continue;
            
            var files = Directory.GetFiles(user.Value, "*.jpeg", SearchOption.AllDirectories)
                .ToList();
            var resultInfos = new List<ScreenshotsInfo>();

            files.ForEach(file =>
            {
                var confFile = file.Replace(".jpeg", ".json");
                var conf = new ConfigEntity<ScreenshotsInfo>(confFile, false);
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
            result.Add("Shared",
                IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ScreenshotFolder));
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
            IsolationCore.GetInstanceUsers(VersionConfig).ForEach(user =>
            {
                result.Add(user,
                    IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ScreenshotFolder, user));
            });

        return result;
    }
}