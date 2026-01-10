using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackManager
{
    public VersionConfig VersionConfig { get; set; }
    public List<ResourcePackManifest> Packs { get; private set; }

    public ResourcePackManager(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public List<ResourcePackManifest> GetAllPack()
    {
        var files = new List<string>();
        var result = new List<ResourcePackManifest>();
        GetInstanceResourcePackPath().Values.ToList().ForEach(folder =>
        {
            var dirs = Directory.GetDirectories(folder).ToList();
            dirs.ForEach(dir =>
            {
                var manifestFile = Path.Combine(dir, "manifest.json");
                if (File.Exists(manifestFile))
                    files.Add(manifestFile);
            });
        });

        GetInstanceBehaviorPackPath().Values.ToList().ForEach(folder =>
        {
            var dirs = Directory.GetDirectories(folder).ToList();
            dirs.ForEach(dir =>
            {
                var manifestFile = Path.Combine(dir, "manifest.json");
                if (File.Exists(manifestFile))
                    files.Add(manifestFile);
            });
        });

        files.ForEach(file => { result.Add(ResourcePackAnalysis.GetPackManifest(file)); });

        Packs = result;

        return result;
    }

    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
    {
        // 获取源目录信息
        var dir = new DirectoryInfo(sourceDir);

        // 检查源目录是否存在
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"源目录不存在: {dir.FullName}");

        // 确保目标目录存在
        Directory.CreateDirectory(destinationDir);

        // 复制所有文件
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // 如果需要递归复制子目录
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    public void AddRangePacks(List<string> files)
    {
        var ids = Packs.Select(p => p.Header.Uuid).ToList();
        files.ForEach(file =>
        {
            var confs = new ResourcePackAnalysis(file).GetPackManifests();
            confs.ForEach(pack =>
            {
                if (!ids.Contains(pack.Header.Uuid))
                {
                    if (pack.PackType == ResourcePackType.Resource)
                    {
                        GetInstanceResourcePackPath().Values.ToList().ForEach(folder =>
                        {
                            if (folder.Contains("Shared") ||
                                VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
                                CopyDirectory(pack.PackRootPath,
                                    Path.Combine(folder, Path.GetFileName(pack.PackRootPath)));
                        });
                    }

                    if (pack.PackType == ResourcePackType.Behavior)
                    {
                        GetInstanceBehaviorPackPath().Values.ToList().ForEach(folder =>
                        {
                            if (folder.Contains("Shared") ||
                                VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
                                CopyDirectory(pack.PackRootPath,
                                    Path.Combine(folder, Path.GetFileName(pack.PackRootPath)));
                        });
                    }
                }
            });
        });
    }

    private Dictionary<string, string> GetInstanceResourcePackPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            result.Add("Shared", Path.Combine(
                IsolationCore.GetRealPath(VersionConfig),
                @"LocalState\games\com.mojang\resource_packs"
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
                    var path = Path.Combine(user, "games", "com.mojang", "resource_packs");
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
                    var path = Path.Combine(user, "games", "com.mojang", "resource_packs");
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

    private Dictionary<string, string> GetInstanceBehaviorPackPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            result.Add("Shared", Path.Combine(
                IsolationCore.GetRealPath(VersionConfig),
                @"LocalState\games\com.mojang\behavior_packs"
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
                    var path = Path.Combine(user, "games", "com.mojang", "behavior_packs");
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
                    var path = Path.Combine(user, "games", "com.mojang", "behavior_packs");
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