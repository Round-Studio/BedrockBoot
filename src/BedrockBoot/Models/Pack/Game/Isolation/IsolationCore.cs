using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockLauncher.Core;
using Round.SDK.Enum;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Models.Pack.Game.Isolation;

public class IsolationCore
{
    public VersionConfig VersionConfig { get; set; }
    public string RealRootPath => Path.Combine(VersionConfig.VersionPath, "config", "BedrockBoot2", "isolation");
    public string RootPath => GetInstanceConfigRootPath();

    public IsolationCore(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public void Init()
    {
        var folderType = DirectoryLinkChecker.CheckFolderType(RootPath);
        if (folderType == DirectoryType.Folder)
            throw new Exception("该实例的目标隔离文件需要进行迁移");
        
        if(!Directory.Exists(RealRootPath))
            Directory.CreateDirectory(RealRootPath);

        if (folderType == DirectoryType.SymbolicLink)
        {
            Directory.Delete(RootPath, true);
            Directory.CreateSymbolicLink(RootPath, RealRootPath);
        }
    }
    
    private string GetInstanceConfigRootPath()
    {
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\Local\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe"
            );
        }
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            if (VersionConfig.Info.VersionType == MinecraftGameTypeVersion.Release)
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
}