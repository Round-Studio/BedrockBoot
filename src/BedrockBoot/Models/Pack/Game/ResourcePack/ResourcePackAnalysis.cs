using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using Octokit;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackAnalysis
{
    public string FilePath { get; private set; }
    private string _tempPath = Path.Combine(PathsList.TempPath, $"pack_{Guid.NewGuid().ToString().Replace("-", "")}");

    public ResourcePackAnalysis(string filePath)
    {
        FilePath = filePath;
    }

    public static ResourcePackType GetPackType(ResourcePackManifest conf)
    {
        var modules = conf.Modules.Select(x => x.Type).ToList();
        if (modules.Contains("resources"))
            return ResourcePackType.Resource;
        if (modules.Contains("data") || modules.Contains("script"))
            return ResourcePackType.Behavior;

        return ResourcePackType.Unknown;
    }

    public ResourcePackType GetPackType()
    {
        var tempPath = _tempPath;
        ZipHelper.ExtractZipFile(FilePath, tempPath);
        var num = Directory.GetDirectories(tempPath).Length;

        if (num == 1 &&
            !File.Exists(Path.Combine(tempPath, "manifest.json")))
            tempPath = Directory.GetDirectories(tempPath)[0];
        
        if (num == 2 &&
            !File.Exists(Path.Combine(tempPath, "manifest.json")))
        {
            return ResourcePackType.Addon; // 直接返回 Addon
        }

        if (num > 2 &&
            !File.Exists(Path.Combine(tempPath, "manifest.json")))
        {
            return ResourcePackType.Unknown;
        }

        var manifestFile = Path.Combine(tempPath, "manifest.json");
        var conf = new ConfigEntity<ResourcePackManifest>(manifestFile, false).Data;

        return GetPackType(conf);
    }

    public List<ResourcePackManifest> GetPackManifests()
    {
        var result = new List<ResourcePackManifest>();
        var type = GetPackType();

        var files = Directory.GetFiles(_tempPath, "manifest.json", SearchOption.AllDirectories);
        files.ToList().ForEach(f=>result.Add(GetPackManifest(f)));

        return result;
    }

    public static ResourcePackManifest GetPackManifest(string file)
    {
        var conf = new ConfigEntity<ResourcePackManifest>(file, false).Data;
        if (conf.Header == null ||
            conf == null)
            return null;
        conf.PackRootPath = Path.GetDirectoryName(file);
        conf.PackType = GetPackType(conf);

        if (conf.Header.Name == "pack.name")
        {
            conf.Header.Name = GetLangText(conf.PackRootPath, "pack.name");
        }

        if (conf.Header.Description == "pack.description")
        {
            conf.Header.Description = GetLangText(conf.PackRootPath, "pack.description");
        }

        return conf;
    }

    private static string GetLangText(string folder, string langKey)
    {
        var textFolder = Path.Combine(folder, "texts");
        var textManifest = Path.Combine(textFolder, "languages.json");

        if (!Directory.Exists(textFolder))
            return langKey;

        var langFiles = new List<string>();

        if (!File.Exists(textManifest))
        {
            var files = Directory.GetFiles(textFolder)
                .Select(f => Path.GetFileName(f).Split('.')[0])
                .ToList();
            langFiles = files;
        }
        else
        {
            var langConf = new ConfigEntity<List<string>>(textManifest, false);
            langFiles = langConf.Data;
        }

        var lang = FindBestMatchLanguage(langFiles);
        var langFile = Path.Combine(textFolder, $"{lang}.lang");

        var langs = File.ReadAllLines(langFile);

        try
        {
            return langs.First(t => t.Contains(langKey))
                .Replace("#", "")
                .Split('=')[1]
                .Replace("\\n", "\n");
        }
        catch
        {
            return langKey;
        }
    }

    private static string FindBestMatchLanguage(List<string> supportedLanguages)
    {
        // 1. 获取当前系统的语言和区域信息
        CultureInfo currentCulture = CultureInfo.CurrentUICulture; // 或者 CultureInfo.CurrentCulture

        // 2. 获取语言代码（不带区域）
        string currentLanguage = currentCulture.TwoLetterISOLanguageName.ToLower();
        string currentFullLocale = currentCulture.Name; // 例如 "zh-CN"

        Console.WriteLine($@"当前系统语言: {currentCulture.DisplayName}");
        Console.WriteLine($@"语言代码: {currentLanguage}, 完整区域: {currentFullLocale}");

        // 3. 优先尝试完全匹配（包括区域）
        string normalizedLocale = currentFullLocale.Replace("-", "_");
        foreach (var lang in supportedLanguages)
        {
            if (string.Equals(lang, normalizedLocale, StringComparison.OrdinalIgnoreCase))
            {
                return lang;
            }
        }

        // 4. 尝试仅匹配语言代码（不带区域）
        foreach (var lang in supportedLanguages)
        {
            if (lang.StartsWith(currentLanguage + "_", StringComparison.OrdinalIgnoreCase))
            {
                return lang;
            }
        }

        // 5. 对于中文的特殊处理（因为中文有多个变体）
        if (currentLanguage == "zh")
        {
            // 根据系统区域决定使用哪种中文变体
            string region = currentFullLocale.Contains("CN") ? "zh_CN" :
                currentFullLocale.Contains("TW") ? "zh_TW" :
                currentFullLocale.Contains("HK") ? "zh_HK" : "zh_CN";

            if (supportedLanguages.Contains(region))
                return region;
        }

        // 6. 如果没有匹配的，返回默认语言（通常是英文）
        return supportedLanguages.Contains("en_US") ? "en_US" : supportedLanguages[0];
    }
}