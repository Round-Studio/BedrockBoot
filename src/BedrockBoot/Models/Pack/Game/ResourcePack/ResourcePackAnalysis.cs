using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackAnalysis
{
    private readonly string _tempPath =
        Path.Combine(PathsList.TempPath, $"pack_{Guid.NewGuid().ToString().Replace("-", "")}");

    public ResourcePackAnalysis(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public static ResourcePackType GetPackType(ResourcePackManifest conf)
    {
        if (conf?.Modules == null) return ResourcePackType.Unknown;
        var modules = conf.Modules.Select(x => x.Type).ToList();
        if (modules.Contains("resources"))
            return ResourcePackType.Resource;
        if (modules.Contains("data") || modules.Contains("script"))
            return ResourcePackType.Behavior;

        return ResourcePackType.Unknown;
    }

    public ResourcePackType GetPackType()
    {
        var manifests = GetPackManifests();
        if (manifests.Count == 0) return ResourcePackType.Unknown;
        if (manifests.Count > 1) return ResourcePackType.Addon;

        var types = manifests.Select(m => m.PackType).Distinct().ToList();
        if (types.Count > 1) return ResourcePackType.Addon;

        return types.FirstOrDefault();
    }

    public PackInfo GetPackInfo()
    {
        if (!Directory.Exists(_tempPath))
        {
            ZipHelper.ExtractZipFile(FilePath, _tempPath);
            ExtractSubPacks(_tempPath);
        }

        var packInfo = new PackInfo { RootPath = _tempPath };

        // 获取所有manifest文件
        var manifestFiles = Directory.GetFiles(_tempPath, "manifest.json", SearchOption.AllDirectories);

        // 构建子包树形结构
        foreach (var file in manifestFiles)
        {
            var manifest = GetPackManifest(file);
            if (manifest != null)
            {
                var directory = Path.GetDirectoryName(file);

                // 判断是否为主包manifest（根目录下的manifest）
                if (directory == _tempPath)
                {
                    packInfo.MainManifest = manifest;
                }
                else
                {
                    // 将非根目录的manifest视为子包
                    var subPackNode = CreateSubPackNode(directory, manifest);
                    packInfo.SubPacks.Add(subPackNode);
                }
            }
        }

        // 构建父子关系
        BuildSubPackHierarchy(packInfo.SubPacks);

        return packInfo;
    }

    private SubPackNode CreateSubPackNode(string directory, ResourcePackManifest manifest)
    {
        var relativePath = Path.GetRelativePath(_tempPath, directory);
        var name = Path.GetFileName(directory);

        return new SubPackNode
        {
            Name = name,
            Path = directory,
            Manifest = manifest,
            Children = new List<SubPackNode>()
        };
    }

    private void BuildSubPackHierarchy(List<SubPackNode> allNodes)
    {
        var nodeMap = allNodes.ToDictionary(n => n.Path, n => n);

        foreach (var node in allNodes.ToList())
        {
            var parentPath = Path.GetDirectoryName(node.Path);

            if (nodeMap.ContainsKey(parentPath))
            {
                nodeMap[parentPath].Children.Add(node);
                allNodes.Remove(node); // 从顶层移除，因为它属于另一个节点的子节点
            }
        }
    }

    public List<ResourcePackManifest> GetPackManifests()
    {
        if (!Directory.Exists(_tempPath))
        {
            ZipHelper.ExtractZipFile(FilePath, _tempPath);
            ExtractSubPacks(_tempPath);
        }

        var result = new List<ResourcePackManifest>();
        var manifestFiles = Directory.GetFiles(_tempPath, "manifest.json", SearchOption.AllDirectories);

        foreach (var file in manifestFiles)
        {
            var manifest = GetPackManifest(file);
            if (manifest != null) result.Add(manifest);
        }

        return result;
    }

    private void ExtractSubPacks(string targetPath)
    {
        var subPacks = Directory.GetFiles(targetPath, "*.mcpack", SearchOption.AllDirectories);
        foreach (var subPack in subPacks)
        {
            var subExtractPath =
                Path.Combine(Path.GetDirectoryName(subPack), Path.GetFileNameWithoutExtension(subPack));
            if (!Directory.Exists(subExtractPath))
            {
                ZipHelper.ExtractZipFile(subPack, subExtractPath);
                // 递归处理嵌套的子包（例如 mcaddon 嵌套了文件夹，文件夹里又有 mcpack）
                ExtractSubPacks(subExtractPath);
            }
        }
    }

    public static ResourcePackManifest GetPackManifest(string file)
    {
        try
        {
            var conf = new ConfigEntity<ResourcePackManifest>(file, false).Data;
            if (conf?.Header == null) return null;

            conf.PackRootPath = Path.GetDirectoryName(file);
            conf.PackType = GetPackType(conf);

            if (conf.Header.Name == "pack.name")
                conf.Header.Name = GetLangText(conf.PackRootPath, "pack.name");

            if (conf.Header.Description == "pack.description")
                conf.Header.Description = GetLangText(conf.PackRootPath, "pack.description");

            return conf;
        }
        catch
        {
            return null;
        }
    }

    private static string GetLangText(string folder, string langKey)
    {
        var textFolder = Path.Combine(folder, "texts");
        if (!Directory.Exists(textFolder)) return langKey;

        var textManifest = Path.Combine(textFolder, "languages.json");
        List<string> langFiles;

        if (!File.Exists(textManifest))
            langFiles = Directory.GetFiles(textFolder, "*.lang")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();
        else
            langFiles = new ConfigEntity<List<string>>(textManifest, false).Data;

        if (langFiles == null || langFiles.Count == 0) return langKey;

        var lang = FindBestMatchLanguage(langFiles);
        var langFile = Path.Combine(textFolder, $"{lang}.lang");

        if (!File.Exists(langFile)) return langKey;

        try
        {
            var lines = File.ReadAllLines(langFile);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("##")) continue;

                var splitIndex = trimmed.IndexOf('=');
                if (splitIndex > 0)
                {
                    var key = trimmed.Substring(0, splitIndex).Trim();
                    if (key == langKey)
                        return trimmed.Substring(splitIndex + 1)
                            .Split('\t')[0]
                            .Split('#')[0]
                            .Trim()
                            .Replace("\\n", "\n");
                }
            }
        }
        catch
        {
            // Ignore
        }

        return langKey;
    }

    private static string FindBestMatchLanguage(List<string> supportedLanguages)
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        var currentLanguage = currentCulture.TwoLetterISOLanguageName.ToLower();
        var currentFullLocale = currentCulture.Name.Replace("-", "_");

        foreach (var lang in supportedLanguages)
            if (string.Equals(lang, currentFullLocale, StringComparison.OrdinalIgnoreCase))
                return lang;

        foreach (var lang in supportedLanguages)
            if (lang.StartsWith(currentLanguage + "_", StringComparison.OrdinalIgnoreCase))
                return lang;

        if (currentLanguage == "zh")
        {
            var region = currentFullLocale.Contains("CN") ? "zh_CN" :
                currentFullLocale.Contains("TW") ? "zh_TW" :
                currentFullLocale.Contains("HK") ? "zh_HK" : "zh_CN";

            if (supportedLanguages.Any(l => string.Equals(l, region, StringComparison.OrdinalIgnoreCase)))
                return supportedLanguages.First(l => string.Equals(l, region, StringComparison.OrdinalIgnoreCase));
        }

        if (supportedLanguages.Contains("en_US")) return "en_US";
        if (supportedLanguages.Contains("en_GB")) return "en_GB";

        return supportedLanguages[0];
    }

    // 新增：重新打包方法
    public void Repack(string outputPath, PackInfo packInfo, bool includeDisabledSubPacks = false)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        // 确保输出目录存在
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        // 创建临时工作目录
        var workPath = Path.Combine(PathsList.TempPath, $"repack_{Guid.NewGuid().ToString().Replace("-", "")}");
        try
        {
            // 复制主包内容
            CopyDirectory(packInfo.RootPath, workPath);

            // 删除所有原始子包文件（.mcpack）
            var originalSubPacks = Directory.GetFiles(workPath, "*.mcpack", SearchOption.AllDirectories);
            foreach (var subPack in originalSubPacks) File.Delete(subPack);

            // 根据子包节点信息重新组织结构
            ProcessSubPacks(workPath, packInfo.SubPacks, includeDisabledSubPacks);

            // 打包成新的zip文件
            ZipHelper.CreateZipFile(workPath, outputPath);
        }
        finally
        {
            // 清理临时工作目录
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    private void ProcessSubPacks(string rootPath, List<SubPackNode> subPacks, bool includeDisabledSubPacks)
    {
        foreach (var subPack in subPacks)
        {
            if (!includeDisabledSubPacks && !subPack.IsEnabled)
                continue;

            // 如果子包有子节点，递归处理
            if (subPack.Children.Any()) ProcessSubPacks(rootPath, subPack.Children, includeDisabledSubPacks);

            // 将子包移动到正确的相对位置
            MoveSubPackToCorrectLocation(rootPath, subPack);
        }
    }

    private void MoveSubPackToCorrectLocation(string rootPath, SubPackNode subPack)
    {
        var sourcePath = subPack.Path;
        var relativePath = Path.GetRelativePath(rootPath, sourcePath);
        var targetPath = Path.Combine(rootPath, relativePath);

        // 如果源路径和目标路径不同，则移动内容
        if (sourcePath != targetPath)
        {
            // 创建目标目录
            Directory.CreateDirectory(targetPath);

            // 移动所有内容
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativeFile = Path.GetRelativePath(sourcePath, file);
                var targetFile = Path.Combine(targetPath, relativeFile);

                var targetDir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                File.Copy(file, targetFile, true);
            }
        }
    }

    private void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");

        if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var targetSubDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, targetSubDir);
        }
    }

    // 新增：子包节点类
    public class SubPackNode
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ResourcePackManifest Manifest { get; set; }
        public List<SubPackNode> Children { get; set; } = new();
        public bool IsEnabled { get; set; } = true; // 是否启用此子包
    }

    // 新增：主包信息类
    public class PackInfo
    {
        public ResourcePackManifest MainManifest { get; set; }
        public List<SubPackNode> SubPacks { get; set; } = new();
        public string RootPath { get; set; }
    }
}