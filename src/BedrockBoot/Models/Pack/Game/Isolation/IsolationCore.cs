using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core;
using Round.SDK.Enum;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Models.Pack.Game.Isolation;

public class IsolationCore
{
    public IsolationCore(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public VersionConfig VersionConfig { get; set; }
    public string RealRootPath => GetRealPath(VersionConfig);
    public string RootPath => GetInstanceConfigRootPath(VersionConfig);

    public void Init(bool isForced = false)
    {
        var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);
        if (folderType == DirectoryType.Folder &&
            !isForced)
            throw new Exception("该实例的目标隔离文件需要进行迁移");

        if (folderType == DirectoryType.SymbolicLink)
            Directory.Delete(RootPath);

        if (!isForced && Directory.Exists(RootPath))
            Directory.Delete(RootPath);

        if (!Directory.Exists(RealRootPath))
            Directory.CreateDirectory(RealRootPath);

        if (folderType == DirectoryType.SymbolicLink)
            try
            {
                Directory.Delete(RootPath, true);
                Directory.CreateSymbolicLink(RootPath, RealRootPath);
            }
            catch
            {
            }

        if (!Directory.Exists(RootPath))
            Directory.CreateSymbolicLink(RootPath, RealRootPath);

        if (DirectoryLinkChecker.CheckFolderType(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock")) ==
            DirectoryType.Folder)
        {
            Directory.Delete(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock"), true);
            Directory.CreateSymbolicLink(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock"), RealRootPath);
        }

        new[]
        {
            "resource_packs",
            "behavior_packs",
            "minecraftWorlds",
            "minecraftpe",
            "custom_skins",
            "skin_packs"
        }.ToList().ForEach(f =>
        {
            if (Directory.Exists(Path.Combine(VersionConfig.VersionPath, f)) &&
                Directory.Exists(GetInstancePackPath(VersionConfig, f)))
            {
                Directory.Delete(Path.Combine(VersionConfig.VersionPath, f), true);
                Directory.CreateSymbolicLink(Path.Combine(VersionConfig.VersionPath, f),
                    GetInstancePackPath(VersionConfig, f));
            }
        });
    }

    public void Clear()
    {
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);

            if (folderType == DirectoryType.SymbolicLink)
                Directory.Delete(RootPath);
        }
    }

    public static string GetRealPath(VersionConfig versionConfig)
    {
        if (versionConfig.Config.IsVersionIsolated)
            return Path.Combine(versionConfig.VersionPath, "config", "BedrockBoot2", "isolation");

        return PathsList.GamePublicRootPath;
    }

    public static string GetInstancePackPath(VersionConfig versionConfig, string folder)
    {
        var root = GetRealPath(versionConfig);
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
            return Path.Combine(root, "Users", "Shared", "games", "com.mojang", folder);

        return Path.Combine(root, "LocalState", "games", "com.mojang", folder);
    }

    public static string GetInstanceConfigRootPath(VersionConfig versionConfig)
    {
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\Local\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe"
            );

        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            if (versionConfig.Info.VersionType == MinecraftGameTypeVersion.Release)
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Minecraft Bedrock"
                );

                return dir;
            }
            else
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Minecraft Bedrock Preview"
                );

                return dir;
            }
        }

        return string.Empty;
    }

    public static string GetInstanceFolderPath(VersionConfig versionConfig,
        InstanceFolderType folderType = InstanceFolderType.RootFolder,
        string user = "Shared")
    {
        return folderType switch
        {
            InstanceFolderType.RootFolder => GetRealPath(versionConfig),
            InstanceFolderType.ResourcePackFolder => GetInstanceFolderPath(versionConfig, "resource_packs", user),
            InstanceFolderType.BehaviorPackFolder => GetInstanceFolderPath(versionConfig, "behavior_packs", user),
            InstanceFolderType.ArchiveFolder => GetInstanceFolderPath(versionConfig, "minecraftWorlds", user),
            InstanceFolderType.OptionFolder => GetInstanceFolderPath(versionConfig, "minecraftpe", user),
            InstanceFolderType.SkinPackFolder => GetInstanceFolderPath(versionConfig, "skin_packs", user),
            InstanceFolderType.UserFolder => new Func<string>(() =>
            {
                if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP) return string.Empty;

                return Path.Combine(GetRealPath(versionConfig), "Users");
            }).Invoke(),
            InstanceFolderType.ScreenshotFolder => GetInstanceFolderPath(versionConfig, "Screenshots", user),
            _ => string.Empty
        };
    }

    public static List<string> GetAllUserFolderPaths(VersionConfig versionConfig,
        InstanceFolderType folderType = InstanceFolderType.RootFolder)
    {
        var users = GetInstanceUsers(versionConfig);
        var result = users.Select(x => GetInstanceFolderPath(versionConfig, folderType, x)).ToList();
        return result;
    }

    public static List<string>? GetInstanceUsers(VersionConfig versionConfig)
    {
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP) return null;

        var userFolder = GetInstanceFolderPath(versionConfig, InstanceFolderType.UserFolder);
        if (Directory.Exists(userFolder))
            return Directory.GetDirectories(userFolder).Select(x => Path.GetFileName(x)).ToList();

        return null;
    }

    private static string GetInstanceFolderPath(VersionConfig VersionConfig, string folder, string user = "Shared")
    {
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return Path.Combine(
                GetRealPath(VersionConfig),
                @"LocalState", "games", "com.mojang",
                folder
            );

        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            var dir = Path.Combine(
                GetRealPath(VersionConfig),
                "Users", user, "games", "com.mojang", folder
            );

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return dir;
        }

        return string.Empty;
    }
}