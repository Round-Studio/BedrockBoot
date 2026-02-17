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
            if (manifest != null)
            {
                result.Add(manifest);
            }
        }

        return result;
    }

    private void ExtractSubPacks(string targetPath)
    {
        var subPacks = Directory.GetFiles(targetPath, "*.mcpack", SearchOption.AllDirectories);
        foreach (var subPack in subPacks)
        {
            var subExtractPath = Path.Combine(Path.GetDirectoryName(subPack), Path.GetFileNameWithoutExtension(subPack));
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
        {
            langFiles = Directory.GetFiles(textFolder, "*.lang")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();
        }
        else
        {
            langFiles = new ConfigEntity<List<string>>(textManifest, false).Data;
        }

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
                    {
                        return trimmed.Substring(splitIndex + 1)
                            .Split('\t')[0]
                            .Split('#')[0]
                            .Trim()
                            .Replace("\\n", "\n");
                    }
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
}