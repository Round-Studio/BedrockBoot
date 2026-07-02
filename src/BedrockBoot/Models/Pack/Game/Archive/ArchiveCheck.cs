using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
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

    public static ArchiveInfo? GetInfo(string save, string? gameFolder = null)
    {
        Console.WriteLine($@"读取存档：{save}");
        
        var file = Directory.EnumerateFiles(save, "level.dat", SearchOption.AllDirectories)
            .FirstOrDefault();
    
        save = Path.GetDirectoryName(file) ?? save;
        
        if (!File.Exists(Path.Combine(save, "levelname.txt")))
            return null;

        var name = File.ReadAllText(Path.Combine(save, "levelname.txt"));

        var icon = Path.Combine(save, "world_icon.jpeg");
        var isProject = false;

        if (Directory.Exists(Path.Combine(save, "editor")))
            isProject = true;

        return new ArchiveInfo
        {
            Name = name,
            Path = Path.Combine(save),
            IconPath = File.Exists(icon) ? icon : "",
            IsProject = isProject,
            LevelWorldData = new ArchiveSerializer(save).Parser(),
            VersionInfo = (gameFolder != null ? GameInfoHelper.GetVersionConfig(gameFolder) : null)!
        };
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
                    var info = GetInfo(save, VersionConfig.VersionPath);
                    if (info != null)
                    {
                        acts.Add(info);
                        var uuidUpdate = info.Uuid;
                    }
                });
            }

            result.Manifest.Add(us.Key, acts);
        });

        return result;
    }

    public void ImportWorldPack(string pack, string user = "Shared")
    {
        if (!pack.EndsWith(".mcworld")) throw new FileNotFoundException(pack);

        var paths = GetInstanceWorldPackPath().ToList();
        paths.ForEach(path =>
        {
            // GDK版本：跳过所有Shared用户的导入
            if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK &&
                path.Key == "Shared")
                return;

            if (!Directory.Exists(path.Value)) Directory.CreateDirectory(path.Value);

            var worldPath = Path.Combine(PathsList.TempPath,
                $"world_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12)}");
            ZipHelper.ExtractZipFile(pack, worldPath);

            worldPath = Path.GetDirectoryName(Directory
                .EnumerateFiles(worldPath, "level.dat", SearchOption.AllDirectories)
                .FirstOrDefault()) ?? worldPath;
            CopyDirectory(worldPath, Path.Combine(path.Value, Path.GetFileName(worldPath)));
        });
    }
    
    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);
            string destFilePath = Path.Combine(destDir, relativePath);
        
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(filePath, destFilePath, true);
        }
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