using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveCheck
{
    public ArchiveCheck(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public VersionConfig VersionConfig { get; set; }

    public ArchiveManifest Check()
    {
        if (VersionConfig == null)
            throw new NullReferenceException("实例配置为空");

        var path = GetInstanceWorldPackPath();
        var result = new ArchiveManifest();

        path.ToList().ForEach(us =>
        {
            var acts = new List<ArchiveInfo>();
            if (Directory.Exists(us.Value))
            {
                var saves = Directory.GetDirectories(us.Value).ToList();
                saves.ForEach(save =>
                {
                    if (!File.Exists(Path.Combine(save, "levelname.txt"))) return;
                    var name = File.ReadAllText(Path.Combine(save, "levelname.txt"));

                    var icon = Path.Combine(save, "world_icon.jpeg");
                    var isProject = false;

                    if (Directory.Exists(Path.Combine(save, "editor")))
                        isProject = true;

                    acts.Add(new ArchiveInfo
                    {
                        Name = name,
                        Path = Path.Combine(save),
                        IconPath = File.Exists(icon) ? icon : "",
                        IsProject = isProject,
                        LevelWorldData = new ArchiveSerializer(save).Parser()
                    });
                });
            }

            result.Manifest.Add(us.Key, acts);
        });

        return result;
    }

    public void ImportWorldPack(string pack, string user = "Shared")
    {
        if (!pack.EndsWith($".mcworld")) throw new FileNotFoundException(pack);

        var paths = GetInstanceWorldPackPath().ToList();
        paths.ForEach(path =>
        {
            // GDK版本：跳过所有Shared用户的导入
            if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK && 
                path.Key == "Shared")
                return;
        
            if (!Directory.Exists(path.Value)) Directory.CreateDirectory(path.Value);
            var worldPath = Path.Combine(path.Value, $"{Guid.NewGuid().ToString().Replace("-", "").Substring(0,12)}");
            ZipHelper.ExtractZipFile(pack, worldPath);
        });
    }

    private Dictionary<string, string> GetInstanceWorldPackPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            result.Add("Shared",
                IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ArchiveFolder));
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
            IsolationCore.GetInstanceUsers(VersionConfig).ForEach(user =>
            {
                result.Add(user,
                    IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ArchiveFolder, user));
            });

        return result;
    }
}