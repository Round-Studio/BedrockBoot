using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Global;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackAnalysis
{
    private string? _tempPath;

    public ResourcePackAnalysis(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    private string TempPath
    {
        get
        {
            _tempPath ??= Path.Combine(PathsList.TempPath, $"pack_{Guid.NewGuid().ToString().Replace("-", "")}");
            return _tempPath;
        }
    }

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
        var _ = TempPath; // 强制初始化 _tempPath，即使 GetPackManifests 提前失败
        var manifests = GetPackManifests();

        var packInfo = new PackInfo { RootPath = _tempPath ?? string.Empty };

        var mainDir = Path.GetDirectoryName(FilePath) ?? string.Empty;

        foreach (var manifest in manifests)
        {
            if (manifest == null) continue;
            if (string.IsNullOrEmpty(manifest.PackRootPath))
            {
                packInfo.MainManifest ??= manifest;
                continue;
            }

            var dir = manifest.PackRootPath;
            if (dir == TempPath || string.IsNullOrEmpty(_tempPath))
            {
                packInfo.MainManifest = manifest;
            }
            else
            {
                var subPackNode = CreateSubPackNode(dir, manifest);
                packInfo.SubPacks.Add(subPackNode);
            }
        }

        BuildSubPackHierarchy(packInfo.SubPacks);
        return packInfo;
    }

    private SubPackNode CreateSubPackNode(string directory, ResourcePackManifest manifest)
    {
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

            if (parentPath != null && nodeMap.ContainsKey(parentPath))
            {
                nodeMap[parentPath].Children.Add(node);
                allNodes.Remove(node);
            }
        }
    }

    public List<ResourcePackManifest> GetPackManifests()
    {
        var result = new List<ResourcePackManifest>();

        try
        {
            using var archive = ZipFile.OpenRead(FilePath);

            // 读取根/子目录下的 manifest.json
            var manifestEntries = archive.Entries
                .Where(e => e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var entry in manifestEntries)
            {
                var manifest = ReadManifestFromZipEntry(archive, entry, TempPath);
                if (manifest == null) continue;

                manifest.PackIconBytes = ReadIconBytesFromZipEntryDir(archive, entry);
                result.Add(manifest);
            }

            // 读取嵌套的 .mcpack 内的 manifest (mcaddon 内含 mcpack)
            var nestedMcpackEntries = archive.Entries
                .Where(e => e.Name.EndsWith(".mcpack", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var entry in nestedMcpackEntries)
            {
                try
                {
                    using var subStream = entry.Open();
                    using var subArchive = new ZipArchive(subStream, ZipArchiveMode.Read);
                    var subManifestEntry = subArchive.Entries
                        .FirstOrDefault(e => e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
                    if (subManifestEntry == null) continue;

                    var entryDir = Path.GetDirectoryName(entry.FullName)?.Replace('\\', '/') ?? "";
                    var subPackRoot = string.IsNullOrEmpty(entryDir)
                        ? Path.Combine(TempPath, Path.GetFileNameWithoutExtension(entry.Name))
                        : Path.Combine(TempPath, entryDir, Path.GetFileNameWithoutExtension(entry.Name));

                    var manifest = ReadManifestFromStream(subManifestEntry.Open(), subPackRoot);
                    if (manifest == null) continue;

                    ResolveI18nFromZip(subArchive, "", manifest);
                    manifest.PackIconBytes = ReadIconBytesFromZip(subArchive);
                    result.Add(manifest);
                }
                catch
                {
                    // 忽略无法读取的子包
                }
            }
        }
        catch
        {
            // 文件无法打开，返回空列表
        }

        return result;
    }

    private static byte[]? ReadIconBytesFromZipEntryDir(ZipArchive archive, ZipArchiveEntry manifestEntry)
    {
        var dir = Path.GetDirectoryName(manifestEntry.FullName)?.Replace('\\', '/') ?? "";
        var iconName = string.IsNullOrEmpty(dir)
            ? "pack_icon.png"
            : $"{dir}/pack_icon.png";
        var iconEntry = archive.GetEntry(iconName)
                       ?? archive.GetEntry(string.IsNullOrEmpty(dir) ? "pack.png" : $"{dir}/pack.png");
        if (iconEntry == null) return null;
        try
        {
            using var ms = new MemoryStream();
            using var s = iconEntry.Open();
            s.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? ReadIconBytesFromZip(ZipArchive archive)
    {
        var iconEntry = archive.Entries
            .FirstOrDefault(e => e.Name.Equals("pack_icon.png", StringComparison.OrdinalIgnoreCase) ||
                                 e.Name.Equals("pack.png", StringComparison.OrdinalIgnoreCase));
        if (iconEntry == null) return null;
        try
        {
            using var ms = new MemoryStream();
            using var s = iconEntry.Open();
            s.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static ResourcePackManifest? ReadManifestFromZipEntry(ZipArchive archive, ZipArchiveEntry entry, string tempPath)
    {
        var entryDir = Path.GetDirectoryName(entry.FullName)?.Replace('\\', '/') ?? "";
        var packRoot = string.IsNullOrEmpty(entryDir)
            ? tempPath
            : Path.Combine(tempPath, entryDir);

        using var stream = entry.Open();
        var manifest = ReadManifestFromStream(stream, packRoot);
        if (manifest != null)
            ResolveI18nFromZip(archive, entryDir, manifest);
        return manifest;
    }

    private static ResourcePackManifest? ReadManifestFromStream(Stream stream, string packRootPath)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            json = SanitizeJsonString(json);
            var conf = JsonSerializer.Deserialize<ResourcePackManifest>(json, JsonSerializerOption.Options);
            if (conf?.Header == null) return null;

            conf.PackRootPath = packRootPath;
            conf.PackType = GetPackType(conf);

            if (conf.Header.Name == "pack.name")
                conf.Header.Name = GetLangText(packRootPath, "pack.name");

            if (conf.Header.Description == "pack.description")
                conf.Header.Description = GetLangText(packRootPath, "pack.description");

            return conf;
        }
        catch
        {
            return null;
        }
    }

    public void ExtractToTemp()
    {
        if (Directory.Exists(TempPath)) return;
        ZipHelper.ExtractZipFile(FilePath, TempPath);
        ExtractSubPacks(TempPath);
    }

    private void ExtractSubPacks(string targetPath)
    {
        var subPacks = Directory.GetFiles(targetPath, "*.mcpack", SearchOption.AllDirectories);
        foreach (var subPack in subPacks)
        {
            var subExtractPath =
                Path.Combine(Path.GetDirectoryName(subPack)!, Path.GetFileNameWithoutExtension(subPack));
            if (!Directory.Exists(subExtractPath))
            {
                ZipHelper.ExtractZipFile(subPack, subExtractPath);
                ExtractSubPacks(subExtractPath);
            }
        }
    }

    public byte[]? GetPackIconBytes()
    {
        try
        {
            using var archive = ZipFile.OpenRead(FilePath);
            var iconEntry = archive.Entries
                .FirstOrDefault(e => e.Name.Equals("pack_icon.png", StringComparison.OrdinalIgnoreCase) ||
                                     e.Name.Equals("pack.png", StringComparison.OrdinalIgnoreCase));
            if (iconEntry == null) return null;

            using var ms = new MemoryStream();
            using var entryStream = iconEntry.Open();
            entryStream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static ResourcePackManifest GetPackManifest(string file)
    {
        try
        {
            var json = File.ReadAllText(file);
            json = SanitizeJsonString(json);
            var conf = JsonSerializer.Deserialize<ResourcePackManifest>(json, JsonSerializerOption.Options);
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

    private static void ResolveI18nFromZip(ZipArchive archive, string entryDir, ResourcePackManifest manifest)
    {
        if (manifest.Header.Name == "pack.name")
            manifest.Header.Name = GetLangTextFromZip(archive, entryDir, "pack.name");
        if (manifest.Header.Description == "pack.description")
            manifest.Header.Description = GetLangTextFromZip(archive, entryDir, "pack.description");
    }

    private static string GetLangTextFromZip(ZipArchive archive, string entryDir, string langKey)
    {
        var textsPrefix = string.IsNullOrEmpty(entryDir) ? "texts/" : $"{entryDir}/texts/";

        List<string> langFiles = [];
        var langManifestEntry = archive.GetEntry($"{textsPrefix}languages.json");
        if (langManifestEntry != null)
        {
            try
            {
                using var reader = new StreamReader(langManifestEntry.Open());
                var json = reader.ReadToEnd();
                langFiles = JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOption.Options) ?? [];
            }
            catch
            {
                // 忽略
            }
        }

        if (langFiles.Count == 0)
        {
            langFiles = archive.Entries
                .Where(e => e.FullName.StartsWith(textsPrefix, StringComparison.OrdinalIgnoreCase) &&
                            e.Name.EndsWith(".lang", StringComparison.OrdinalIgnoreCase))
                .Select(e => Path.GetFileNameWithoutExtension(e.Name))
                .ToList();
        }

        if (langFiles.Count == 0) return langKey;

        var lang = FindBestMatchLanguage(langFiles);
        var langEntry = archive.GetEntry($"{textsPrefix}{lang}.lang");
        if (langEntry == null) return langKey;

        try
        {
            using var reader = new StreamReader(langEntry.Open());
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var trimmed = line.Trim();
                if (trimmed.StartsWith("##")) continue;

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

    public void Repack(string outputPath, PackInfo packInfo, bool includeDisabledSubPacks = false)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        var workPath = Path.Combine(PathsList.TempPath, $"repack_{Guid.NewGuid().ToString().Replace("-", "")}");
        try
        {
            ExtractToTemp();

            CopyDirectory(TempPath, workPath);

            var originalSubPacks = Directory.GetFiles(workPath, "*.mcpack", SearchOption.AllDirectories);
            foreach (var subPack in originalSubPacks) File.Delete(subPack);

            ProcessSubPacks(workPath, packInfo.SubPacks, includeDisabledSubPacks);

            ZipHelper.CreateZipFile(workPath, outputPath);
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    private void ProcessSubPacks(string rootPath, List<SubPackNode> subPacks, bool includeDisabledSubPacks)
    {
        foreach (var subPack in subPacks)
        {
            if (!includeDisabledSubPacks && !subPack.IsEnabled)
                continue;

            if (subPack.Children.Any()) ProcessSubPacks(rootPath, subPack.Children, includeDisabledSubPacks);
            MoveSubPackToCorrectLocation(rootPath, subPack);
        }
    }

    private void MoveSubPackToCorrectLocation(string rootPath, SubPackNode subPack)
    {
        var sourcePath = subPack.Path;
        var relativePath = Path.GetRelativePath(rootPath, sourcePath);
        var targetPath = Path.Combine(rootPath, relativePath);

        if (sourcePath != targetPath)
        {
            Directory.CreateDirectory(targetPath);

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

    private static string SanitizeJsonString(string json)
    {
        var sb = new StringBuilder(json.Length);
        var inString = false;
        var escape = false;

        foreach (var c in json)
        {
            if (escape)
            {
                escape = false;
                sb.Append(c);
                continue;
            }

            if (inString)
            {
                if (c == '\\')
                {
                    escape = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    sb.Append(c);
                    continue;
                }

                if (c == '\r')
                {
                    sb.Append("\\r");
                    continue;
                }

                if (c == '\n')
                {
                    sb.Append("\\n");
                    continue;
                }

                sb.Append(c);
            }
            else
            {
                if (c == '"') inString = true;
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public class SubPackNode
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ResourcePackManifest Manifest { get; set; }
        public List<SubPackNode> Children { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    public class PackInfo
    {
        public ResourcePackManifest MainManifest { get; set; }
        public List<SubPackNode> SubPacks { get; set; } = new();
        public string RootPath { get; set; }
    }
}
