using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core;
using Round.SDK.Enum;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Models.Pack.Game.Isolation;

public class IsolationCore
{
    public VersionConfig VersionConfig { get; set; }
    public string RealRootPath => GetRealPath(VersionConfig);
    public string RootPath => GetInstanceConfigRootPath(VersionConfig);

    public IsolationCore(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public void Init()
    {
        var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);
        if (folderType == DirectoryType.Folder)
            throw new Exception("该实例的目标隔离文件需要进行迁移");

        if (folderType == DirectoryType.SymbolicLink)
            Directory.Delete(RootPath);
        
        if(!Directory.Exists(RealRootPath))
            Directory.CreateDirectory(RealRootPath);

        if (folderType == DirectoryType.SymbolicLink)
        {
            Directory.Delete(RootPath, true);
            Directory.CreateSymbolicLink(RootPath, RealRootPath);
        }
        
        if(!Directory.Exists(RootPath))
            Directory.CreateSymbolicLink(RootPath, RealRootPath);

        if (DirectoryLinkChecker.CheckFolderType(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock")) ==
            DirectoryType.Folder)
        {
            Directory.Delete(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock"), true);
            Directory.CreateSymbolicLink(Path.Combine(VersionConfig.VersionPath, "Minecraft Bedrock"), RealRootPath);
        }
    }

    public static string GetRealPath(VersionConfig versionConfig)
    {
        if (versionConfig.Config.IsVersionIsolated)
            return Path.Combine(versionConfig.VersionPath, "config", "BedrockBoot2", "isolation");

        return PathsList.GamePublicRootPath;
    }
    
    public static string GetInstanceConfigRootPath(VersionConfig versionConfig)
    {
        if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\Local\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe"
            );
        }
        else if (versionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
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

    public void Clear()
    {
        var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);

        if (folderType == DirectoryType.SymbolicLink)
            Directory.Delete(RootPath);
    }
}