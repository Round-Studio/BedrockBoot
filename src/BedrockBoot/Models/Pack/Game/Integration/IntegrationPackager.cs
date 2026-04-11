using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Isolation;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Integration;

public class IntegrationPackager
{
    public IntegrationPackager(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public VersionConfig VersionConfig { get; set; }
    public IProgress<IntegrationProgress>? IntegrationProgress { get; set; }
    public Action? CompleteCallBack { get; set; }

    public void BeginPack(PackInfo config)
    {
        IntegrationProgress?.Report(new IntegrationProgress
        {
            Progress = 10,
            Message = "获取版本详细信息"
        });

        var gameVersions = VersionHelper.GetVersions()
            .Find(x => x.ID.Replace(".", "") ==
                       GameInfoHelper.GetVersionConfig(VersionConfig.VersionPath).Info.Version.Replace(".", ""));
        
        if(gameVersions==null) throw new Exception("无法获取版本详细信息");

        config.VersionInfo = new PackInfo.GameVersionInfo()
        {
            Version = gameVersions.ID.Replace(".", ""),
            BuildType = gameVersions.BuildType.ToString()
        };
        
        var path = Path.Combine(PathsList.TempPath, $"integration_{Guid.NewGuid().ToString().Replace("-", "")}");
        Directory.CreateDirectory(path);

        var conf = new ConfigEntity<PackInfo>(Path.Combine(path, "pack.json"));
        conf.Data = config;
        conf.Save();

        if (!string.IsNullOrEmpty(config.PackIconFile) &&
            File.Exists(config.PackIconFile))
            File.Copy(config.PackIconFile, Path.Combine(path, "pack_icon.png"));

        if (config.EnableConfig.IsEnableDllFile)
            Directory.CreateDirectory(Path.Combine(path, "mods"));
        if (config.EnableConfig.IsEnableResourcePack)
            Directory.CreateDirectory(Path.Combine(path, "packs", "resource_packs"));
        if (config.EnableConfig.IsEnableBehaviorPack)
            Directory.CreateDirectory(Path.Combine(path, "packs", "behavior_packs"));
        if (config.EnableConfig.IsEnableSkinPack)
            Directory.CreateDirectory(Path.Combine(path, "packs", "skin_packs"));
        if (config.EnableConfig.IsEnableArchive)
            Directory.CreateDirectory(Path.Combine(path, "worlds"));

        IntegrationProgress?.Report(new IntegrationProgress
        {
            Progress = 40,
            Message = "目录创建完毕"
        });

        if (config.EnableConfig.IsEnableResourcePack)
        {
            var resPackFolders =
                IsolationCore.GetAllUserFolderPaths(VersionConfig, InstanceFolderType.ResourcePackFolder);
            var packFolders = resPackFolders
                .SelectMany(pa => Directory.GetDirectories(pa))
                .ToList();

            var count = 0;
            packFolders.ForEach(pa =>
            {
                var file = Path.Combine(path, "packs", "resource_packs",
                    $"integration_pack_{Guid.NewGuid().ToString().Replace("-", "")}.mcpack");
                Console.WriteLine($@"正在处理资源包：{file}");
                ZipHelper.CreateZipFile(pa, file);
                count++;
                IntegrationProgress?.Report(new IntegrationProgress
                {
                    Message = $@"正在处理资源包：{Path.GetFileName(file)}",
                    Progress = (double)count / packFolders.Count * 100
                });
            });
        }

        if (config.EnableConfig.IsEnableBehaviorPack)
        {
            var behPackFolders =
                IsolationCore.GetAllUserFolderPaths(VersionConfig, InstanceFolderType.BehaviorPackFolder);
            var packFolders = behPackFolders
                .SelectMany(pa => Directory.GetDirectories(pa))
                .ToList();

            var count = 0;
            packFolders.ForEach(pa =>
            {
                var file = Path.Combine(path, "packs", "behavior_packs",
                    $"integration_pack_{Guid.NewGuid().ToString().Replace("-", "")}.mcpack");
                Console.WriteLine($@"正在处理行为包：{file}");
                ZipHelper.CreateZipFile(pa, file);
                count++;
                IntegrationProgress?.Report(new IntegrationProgress
                {
                    Message = $@"正在处理行为包：{Path.GetFileName(file)}",
                    Progress = (double)count / packFolders.Count * 100
                });
            });
        }

        if (config.EnableConfig.IsEnableSkinPack)
        {
            var skiPackFolders = IsolationCore.GetAllUserFolderPaths(VersionConfig, InstanceFolderType.SkinPackFolder);
            var packFolders = skiPackFolders
                .SelectMany(pa => Directory.GetDirectories(pa))
                .ToList();

            var count = 0;
            packFolders.ForEach(pa =>
            {
                var file = Path.Combine(path, "packs", "skin_packs",
                    $"integration_pack_{Guid.NewGuid().ToString().Replace("-", "")}.mcpack");
                Console.WriteLine($@"正在处理皮肤包：{file}");
                ZipHelper.CreateZipFile(pa, file);
                count++;
                IntegrationProgress?.Report(new IntegrationProgress
                {
                    Message = $@"正在处理皮肤包：{Path.GetFileName(file)}",
                    Progress = (double)count / packFolders.Count * 100
                });
            });
        }

        if (config.EnableConfig.IsEnableArchive)
        {
            var arcPackFolders = IsolationCore.GetAllUserFolderPaths(VersionConfig, InstanceFolderType.ArchiveFolder);
            var packFolders = arcPackFolders
                .SelectMany(pa => Directory.GetDirectories(pa))
                .ToList();

            var count = 0;
            packFolders.ForEach(pa =>
            {
                var file = Path.Combine(path, "worlds", $"{Path.GetFileName(pa)}.mcworld");
                Console.WriteLine($@"正在处理存档：{file}");
                ZipHelper.CreateZipFile(pa, file);
                count++;
                IntegrationProgress?.Report(new IntegrationProgress
                {
                    Message = $@"正在处理存档：{Path.GetFileName(file)}",
                    Progress = (double)count / packFolders.Count * 100
                });
            });
        }

        if (config.EnableConfig.IsEnableDllFile)
        {
            var modManager = new ModsManager(VersionConfig);
            modManager.RefreshMods();
            var count = 0;
            var modConf =
                new ConfigEntity<Dictionary<string, PackModInfo>>(Path.Combine(path, "mods", "mods.json"));
            modManager.Mods.ForEach(mod =>
            {
                var file = mod.File;
                count++;
                IntegrationProgress?.Report(new IntegrationProgress
                {
                    Message = $@"正在处理 dll 文件：{Path.GetFileName(file)}",
                    Progress = (double)count / modManager.Mods.Count * 100
                });
                File.Copy(file, Path.Combine(path, "mods", Path.GetFileName(file)));
                modConf.Data = new Dictionary<string, PackModInfo>();
                modConf.Data.Add(Path.GetFileName(file), new PackModInfo
                {
                    Delay = mod.InjectDelay,
                    IsPreLoad = mod.IsPreLoad
                });
            });
            modConf.Save();
        }

        IntegrationProgress?.Report(new IntegrationProgress
        {
            Progress = 80,
            Message = "打包文件中..."
        });

        ZipHelper.CreateZipFile(path, config.PackSavePath);

        IntegrationProgress?.Report(new IntegrationProgress
        {
            Progress = 100,
            Message = "整合包打包完毕"
        });
        
        CompleteCallBack?.Invoke();
    }
}