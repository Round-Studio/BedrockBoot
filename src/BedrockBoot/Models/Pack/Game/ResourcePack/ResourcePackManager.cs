using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackManager
{
    public ResourcePackManager(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
        Packs = new List<ResourcePackManifest>();
    }

    public VersionConfig VersionConfig { get; set; }
    public List<ResourcePackManifest> Packs { get; private set; }

    private List<string> GetManifests(string dir)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return new List<string>();

        return Directory.GetFiles(dir, "manifest.json", SearchOption.AllDirectories).ToList();
    }

    public List<ResourcePackManifest> GetAllPack(string user = "Shared")
    {
        var files = new List<string>();
        var result = new List<ResourcePackManifest>();

        // 获取资源包目录
        var resourcePackDir =
            IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ResourcePackFolder, user);
        if (!string.IsNullOrEmpty(resourcePackDir) && Directory.Exists(resourcePackDir))
            Directory.GetDirectories(resourcePackDir).ToList()
                .ForEach(dir => { files.AddRange(GetManifests(dir)); });

        // 获取行为包目录
        var behaviorPackDir =
            IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.BehaviorPackFolder, user);
        if (!string.IsNullOrEmpty(behaviorPackDir) && Directory.Exists(behaviorPackDir))
            Directory.GetDirectories(behaviorPackDir).ToList()
                .ForEach(dir => { files.AddRange(GetManifests(dir)); });

        // 获取皮肤包目录
        var skinPackDir = IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.SkinPackFolder, user);
        if (!string.IsNullOrEmpty(skinPackDir) && Directory.Exists(skinPackDir))
            Directory.GetDirectories(skinPackDir).ToList()
                .ForEach(dir => { files.AddRange(GetManifests(dir)); });

        // 获取世界模板包目录
        var templatePackDir =
            IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.WorldTemplateFolder, user);
        if (!string.IsNullOrEmpty(templatePackDir) && Directory.Exists(templatePackDir))
            Directory.GetDirectories(templatePackDir).ToList()
                .ForEach(dir => { files.AddRange(GetManifests(dir)); });

        files.ForEach(file =>
        {
            try
            {
                var manifest = ResourcePackAnalysis.GetPackManifest(file);
                if (manifest != null)
                    // 确保Header不为空
                    if (manifest.Header != null && !string.IsNullOrEmpty(manifest.Header.Uuid))
                        result.Add(manifest);
            }
            catch (Exception ex)
            {
                // 记录错误但继续处理其他文件
                Console.WriteLine($@"Error processing manifest {file}: {ex.Message}");
            }
        });

        Packs = result;
        return result;
    }

    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"源目录不存在: {dir.FullName}");

        Directory.CreateDirectory(destinationDir);
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

    public void AddRangePacks(List<string> files, string user = "Shared")
    {
        if (files == null || !files.Any())
            return;

        // 重新获取现有包列表以确保Packs不为空
        if (Packs == null)
            Packs = new List<ResourcePackManifest>();

        var ids = new HashSet<string>(Packs.Where(p => p?.Header != null).Select(p => p.Header.Uuid)
            .Where(id => !string.IsNullOrEmpty(id)));

        files.ForEach(file =>
        {
            try
            {
                var analysis = new ResourcePackAnalysis(file);
                var confs = analysis.GetPackManifests();

                if (confs == null || confs.Count == 0) return;

                // 需要安装时才解压到临时目录
                analysis.ExtractToTemp();

                confs.ForEach(pack =>
                {
                    if (pack != null && pack.Header != null && !string.IsNullOrEmpty(pack.Header.Uuid))
                        if (!ids.Contains(pack.Header.Uuid))
                            if (!string.IsNullOrEmpty(pack.PackRootPath) && Directory.Exists(pack.PackRootPath))
                            {
                                if (pack.PackType == ResourcePackType.Resource)
                                {
                                    var resourcePackDir = IsolationCore.GetInstanceFolderPath(VersionConfig,
                                        InstanceFolderType.ResourcePackFolder, user);
                                    if (!string.IsNullOrEmpty(resourcePackDir))
                                    {
                                        var destPath = Path.Combine(resourcePackDir,
                                            Path.GetFileName(pack.PackRootPath));
                                        CopyDirectory(pack.PackRootPath, destPath);
                                    }
                                }
                                else if (pack.PackType == ResourcePackType.Behavior)
                                {
                                    var behaviorPackDir = IsolationCore.GetInstanceFolderPath(VersionConfig,
                                        InstanceFolderType.BehaviorPackFolder, user);
                                    if (!string.IsNullOrEmpty(behaviorPackDir))
                                    {
                                        var destPath = Path.Combine(behaviorPackDir,
                                            Path.GetFileName(pack.PackRootPath));
                                        CopyDirectory(pack.PackRootPath, destPath);
                                    }
                                }
                                else if (pack.PackType == ResourcePackType.Skin)
                                {
                                    var skinPackDir = IsolationCore.GetInstanceFolderPath(VersionConfig,
                                        InstanceFolderType.SkinPackFolder, user);
                                    if (!string.IsNullOrEmpty(skinPackDir))
                                    {
                                        var destPath = Path.Combine(skinPackDir,
                                            Path.GetFileName(pack.PackRootPath));
                                        CopyDirectory(pack.PackRootPath, destPath);
                                    }
                                }
                                else if (pack.PackType == ResourcePackType.WorldTemplate)
                                {
                                    var templatePackDir = IsolationCore.GetInstanceFolderPath(VersionConfig,
                                        InstanceFolderType.WorldTemplateFolder, user);
                                    if (!string.IsNullOrEmpty(templatePackDir))
                                    {
                                        var destPath = Path.Combine(templatePackDir,
                                            Path.GetFileName(pack.PackRootPath));
                                        CopyDirectory(pack.PackRootPath, destPath);
                                    }
                                }

                                // 添加到ID集合，避免重复
                                ids.Add(pack.Header.Uuid);
                            }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error adding pack from {file}: {ex.Message}");
            }
        });

        // 重新获取所有包以更新Packs列表
        GetAllPack();
    }
}