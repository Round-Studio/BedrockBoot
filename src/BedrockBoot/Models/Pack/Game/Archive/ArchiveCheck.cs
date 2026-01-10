using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveCheck
{
    public VersionConfig VersionConfig { get; set; }

    public ArchiveCheck()
    {

    }

    public ArchiveCheck(VersionConfig versionConfig) : this()
    {
        VersionConfig = versionConfig;
    }

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

                    acts.Add(new ArchiveInfo()
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

    private Dictionary<string, string> GetInstanceWorldPackPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            result.Add("Shared", Path.Combine(
                IsolationCore.GetRealPath(VersionConfig),
                @"LocalState\games\com.mojang\minecraftWorlds"
            ));
        }
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            if (VersionConfig.Info.VersionType == MinecraftGameTypeVersion.Release)
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
                    var path = Path.Combine(user, "games", "com.mojang", "minecraftWorlds");
                    if (Path.Exists(path))
                        result.Add(Path.GetFileName(user), path);
                    else
                    {
                        Directory.CreateDirectory(path);
                        result.Add(Path.GetFileName(user), path);
                    }
                });
            }
            else
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
                    var path = Path.Combine(user, "games", "com.mojang", "minecraftWorlds");
                    if (Path.Exists(path))
                        result.Add(Path.GetFileName(user), path);
                    else
                    {
                        Directory.CreateDirectory(path);
                        result.Add(Path.GetFileName(user), path);
                    }
                });
            }
        }

        return result;
    }
}