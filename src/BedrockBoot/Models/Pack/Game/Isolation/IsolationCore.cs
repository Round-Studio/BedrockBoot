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

    private void SafeDeleteIfSymbolicLink(string path)
    {
        if (!Directory.Exists(path)) return;

        var type = DirectoryLinkChecker.CheckFolderType(path);
        if (type == DirectoryType.SymbolicLink)
        {
            Directory.Delete(path);
        }
        else
        {
            throw new InvalidOperationException(
                $"目标路径 '{path}' 是真实目录，无法安全隔离。请启用强制模式或手动清理。");
        }
    }

    private void CreateSymbolicLinkSafe(string linkPath, string targetPath)
    {
        if (Directory.Exists(linkPath))
            SafeDeleteIfSymbolicLink(linkPath);

        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException(
                "无法创建符号链接：请以管理员身份运行程序，或在 Windows 设置中启用「开发者模式」。", ex);
        }
        catch (IOException ex) when (ex.Message.Contains("privilege") || ex.HResult == -2147024891)
        {
            throw new InvalidOperationException(
                "创建符号链接需要提升权限或启用开发者模式。", ex);
        }
    }

    public void Init(bool isForced = false)
    {
        Clear();

        var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);
        if (folderType == DirectoryType.Folder && !isForced)
            throw new Exception("该实例的目标隔离文件需要进行迁移");

        // 处理 LocalState
        var localStatePath = Path.Combine(RealRootPath, "LocalState");
        if (Path.Exists(localStatePath))
        {
            if (DirectoryLinkChecker.CheckFolderType(localStatePath) == DirectoryType.SymbolicLink)
            {
                Directory.Delete(localStatePath);
                Directory.CreateDirectory(localStatePath);
            }
        }
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            Directory.CreateDirectory(localStatePath);
        }

        // 清理 RootPath（仅符号链接）
        if (folderType == DirectoryType.SymbolicLink)
            SafeDeleteIfSymbolicLink(RootPath);

        // 创建 RealRootPath
        if (!Directory.Exists(RealRootPath))
            Directory.CreateDirectory(RealRootPath);

        // 创建符号链接
        CreateSymbolicLinkSafe(RootPath, RealRootPath);

        // 链接 "Minecraft Bedrock"
        var bedrockLinkPath = Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock");
        if (DirectoryLinkChecker.CheckFolderType(bedrockLinkPath) == DirectoryType.Folder)
        {
            Directory.Delete(bedrockLinkPath, true);
        }
        if (!Directory.Exists(bedrockLinkPath))
        {
            CreateSymbolicLinkSafe(bedrockLinkPath, RealRootPath);
        }

        // 链接子目录
        var packFolders = new[]
        {
            "resource_packs",
            "behavior_packs",
            "minecraftWorlds",
            "minecraftpe",
            "custom_skins",
            "skin_packs"
        };

        foreach (var f in packFolders)
        {
            var versionPackPath = Path.Combine(VersionConfig.VersionPath, f);
            var instancePackPath = GetInstancePackPath(VersionConfig, f);

            if (Directory.Exists(versionPackPath))
                Directory.Delete(versionPackPath, true);

            if (Directory.Exists(instancePackPath))
            {
                CreateSymbolicLinkSafe(versionPackPath, instancePackPath);
            }
        }
    }

    public void Clear()
    {
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            SafeDeleteIfSymbolicLink(RootPath);
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
            string dirName = versionConfig.Info.VersionType == MinecraftGameTypeVersion.Release
                ? "Minecraft Bedrock"
                : "Minecraft Bedrock Preview";

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
                : Path.Combine(GetRealPath(versionConfig), "Users"),
            InstanceFolderType.ScreenshotFolder => GetInstanceFolderPath(versionConfig, "Screenshots", user),
            _ => string.Empty
        };
    }

    public static List<string> GetAllUserFolderPaths(VersionConfig versionConfig,
        InstanceFolderType folderType = InstanceFolderType.RootFolder)
    {
        var users = GetInstanceUsers(versionConfig);
        return users.Select(x => GetInstanceFolderPath(versionConfig, folderType, x)).ToList();
    }

    public static List<string> GetInstanceUsers(VersionConfig versionConfig)
    {
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return new() { "Shared" };

        var userFolder = GetInstanceFolderPath(versionConfig, InstanceFolderType.UserFolder);
        if (Directory.Exists(userFolder))
            return Directory.GetDirectories(userFolder).Select(Path.GetFileName).ToList();

        return new() { "Shared" };
    }

    private static string GetInstanceFolderPath(VersionConfig versionConfig, string folder, string user = "Shared")
    {
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            return Path.Combine(GetRealPath(versionConfig), @"LocalState", "games", "com.mojang", folder);

        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            var dir = Path.Combine(GetRealPath(versionConfig), "Users", user, "games", "com.mojang", folder);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        throw new NotSupportedException($"不支持的 Minecraft 构建类型: {versionConfig.Info.BuildType}");
    }
}