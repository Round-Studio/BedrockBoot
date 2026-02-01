using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackManager
{
    public ResourcePackManager(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public VersionConfig VersionConfig { get; set; }
    public List<ResourcePackManifest> Packs { get; private set; }

    private List<string> GetManifests(string dir)
    {
        return Directory.GetFiles(dir, "manifest.json", SearchOption.AllDirectories).ToList();
    }

    public List<ResourcePackManifest> GetAllPack()
    {
        var files = new List<string>();
        var result = new List<ResourcePackManifest>();

        Directory.GetDirectories(IsolationCore.GetInstanceFolderPath(VersionConfig,
                InstanceFolderType.ResourcePackFolder)).ToList()
            .ForEach(dir => { files.AddRange(GetManifests(dir)); });
        Directory.GetDirectories(IsolationCore.GetInstanceFolderPath(VersionConfig,
                InstanceFolderType.BehaviorPackFolder)).ToList()
            .ForEach(dir => { files.AddRange(GetManifests(dir)); });

        files.ForEach(file =>
        {
            var con = ResourcePackAnalysis.GetPackManifest(file);
            if (con != null)
                result.Add(ResourcePackAnalysis.GetPackManifest(file));
        });

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
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // 如果需要递归复制子目录
        if (recursive)
            foreach (var subDir in dir.GetDirectories())
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
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
                        CopyDirectory(pack.PackRootPath,
                            Path.Combine(
                                IsolationCore.GetInstanceFolderPath(VersionConfig,
                                    InstanceFolderType.ResourcePackFolder), Path.GetFileName(pack.PackRootPath)));

                    if (pack.PackType == ResourcePackType.Behavior)
                        CopyDirectory(pack.PackRootPath,
                            Path.Combine(
                                IsolationCore.GetInstanceFolderPath(VersionConfig,
                                    InstanceFolderType.BehaviorPackFolder), Path.GetFileName(pack.PackRootPath)));
                }
            });
        });
    }
}