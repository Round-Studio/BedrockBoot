using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Models.Helper;
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
        var gameFolderType = GameInfoHelper.GetVersionRootFolderType(VersionConfig);
        if (gameFolderType == GameFolderType.LeviLauncher)
        {
            var llIsoFolder = Path.Combine(versionConfig.VersionPath,
                versionConfig.Info.VersionType == MinecraftGameTypeVersion.Release
                    ? "Minecraft Bedrock"
                    : "Minecraft Bedrock Preview");

            var bbIsoFolder = GetRealPath(versionConfig);

            if (Directory.Exists(bbIsoFolder) || File.Exists(bbIsoFolder))
            {
                var attr = File.GetAttributes(bbIsoFolder);
                if ((attr & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        Directory.Delete(bbIsoFolder, true);
                    }
                    else
                    {
                        File.Delete(bbIsoFolder);
                    }
                }
            }

            try
            {
                Directory.CreateSymbolicLink(bbIsoFolder, llIsoFolder);
            }
            catch(Exception exception)
            {
                Console.WriteLine($@"创建符号链接失败：{exception}");
            }
        }
    }

    public VersionConfig VersionConfig { get; set; }

    public static string GetRealPath(VersionConfig versionConfig)
    {
        if (versionConfig.Config.IsVersionIsolated)
            return Path.Combine(versionConfig.VersionPath, "config", "BedrockBoot2", "isolation");

#if LINUX
        return Path.Combine(PathsList.ProtonPath, "game_prefix", "pfx", "drive_c", "users", "steamuser", "AppData",
            "Roaming", versionConfig.Info.VersionType == BedrockLauncher.Core.MinecraftGameTypeVersion.Release
                ? "Minecraft Bedrock"
                : "Minecraft Bedrock Preview");
#endif
        return PathsList.GamePublicRootPath;
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
            var dirName = versionConfig.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "Minecraft Bedrock"
                : "Minecraft Bedrock Preview";

#if LINUX
            return Path.Combine(PathsList.ProtonPath, "game_prefix", "pfx", "drive_c", "users", "steamuser", "AppData",
                "Roaming", dirName);
#endif

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                dirName
            );
        }

        throw new NotSupportedException($"不支持的 Minecraft 构建类型: {versionConfig.Info.BuildType}");
    }

    public static string GetInstanceFolderPath(VersionConfig versionConfig,
        InstanceFolderType folderType = InstanceFolderType.RootFolder,
        string user = "Shared")
    {
        var core = new IsolationCore(versionConfig);
        return folderType switch
        {
            InstanceFolderType.RootFolder => GetRealPath(versionConfig),
            InstanceFolderType.ResourcePackFolder => GetInstanceFolderPath(versionConfig, "resource_packs", user),
            InstanceFolderType.BehaviorPackFolder => GetInstanceFolderPath(versionConfig, "behavior_packs", user),
            InstanceFolderType.ArchiveFolder => GetInstanceFolderPath(versionConfig, "minecraftWorlds", user),
            InstanceFolderType.OptionFolder => GetInstanceFolderPath(versionConfig, "minecraftpe", user),
            InstanceFolderType.SkinPackFolder => GetInstanceFolderPath(versionConfig, "skin_packs", user),
            InstanceFolderType.UserFolder => versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP
                ? string.Empty
                : versionConfig.Info.VersionType == MinecraftGameTypeVersion.Preview
                    ? Path.Combine(GetRealPath(versionConfig), " Preview", "Users")
                    : Path.Combine(GetRealPath(versionConfig), "Users"),
            InstanceFolderType.ScreenshotFolder => GetInstanceFolderPath(versionConfig, "Screenshots", user),
            _ => string.Empty
        };
    }

    public static List<string> GetAllUserFolderPaths(VersionConfig versionConfig,
        InstanceFolderType folderType = InstanceFolderType.RootFolder)
    {
        Console.WriteLine(@"获取实例所有用户的目标文件夹路径，folderType: {0}", folderType);
        
        var users = GetInstanceUsers(versionConfig);
        return users.Select(x => GetInstanceFolderPath(versionConfig, folderType, x)).ToList();
    }

    public static List<string> GetInstanceUsers(VersionConfig versionConfig)
    {
        Console.WriteLine(@"获取实例所有用户");
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return new List<string> { "Shared" };

        var userFolder = GetInstanceFolderPath(versionConfig, InstanceFolderType.UserFolder);
        if (Directory.Exists(userFolder))
            return Directory.GetDirectories(userFolder).Select(Path.GetFileName).ToList();

        return new List<string> { "Shared" };
    }

    private static string GetInstanceFolderPath(VersionConfig versionConfig, string folder, string user = "Shared")
    {
        Console.WriteLine("获取目标实例的文件夹路径，folder: {0}, user: {1}", folder, user);
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return Path.Combine(GetRealPath(versionConfig), @"LocalState", "games", "com.mojang", folder);

        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            if (versionConfig.Info.VersionType == MinecraftGameTypeVersion.Preview ||
                versionConfig.Info.VersionType == MinecraftGameTypeVersion.Beta)
            {
                var dir = Path.Combine(GetRealPath(versionConfig), " Preview", "Users", user, "games", "com.mojang",
                    folder);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
            else
            {
                var dir = Path.Combine(GetRealPath(versionConfig), "Users", user, "games", "com.mojang", folder);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        throw new NotSupportedException($"不支持的 Minecraft 构建类型: {versionConfig.Info.BuildType}");
    }
}