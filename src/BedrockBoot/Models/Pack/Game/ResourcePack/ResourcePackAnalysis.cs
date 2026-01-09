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
        ZipHelper.ExtractZipFile(FilePath, _tempPath);
        var num = Directory.GetDirectories(_tempPath).Length;
        if (num == 2 &&
            !File.Exists(Path.Combine(_tempPath, "manifest.json")))
        {
            return ResourcePackType.Addon; // 直接返回 Addon
        }
        if (num > 2 &&
             !File.Exists(Path.Combine(_tempPath, "manifest.json")))
        {
            return ResourcePackType.Unknown;
        }

        var manifestFile = Path.Combine(_tempPath, "manifest.json");
        var conf = new ConfigEntity<ResourcePackManifest>(manifestFile).Data;

        return GetPackType(conf);
    }

    public List<ResourcePackManifest> GetPackManifests()
    {
        var result = new List<ResourcePackManifest>();
        var type = GetPackType();

        if (type == ResourcePackType.Resource ||
            type == ResourcePackType.Behavior)
        {
            var manifestFile = Path.Combine(_tempPath, "manifest.json");
            result.Add(GetPackManifest(manifestFile));
        }
        else if(type == ResourcePackType.Addon)
        {
            var folder = Directory.GetDirectories(_tempPath);
            var files = folder.Select(f => Path.Combine(f, "manifest.json")).ToList();
            
            files.ForEach(f =>
            {
                result.Add(GetPackManifest(f));
            });
        }
        else
        {
            return null;
        }

        return result;
    }

    public static ResourcePackManifest GetPackManifest(string file)
    {
        var conf = new ConfigEntity<ResourcePackManifest>(file).Data;
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
        if (!File.Exists(textManifest))
            return langKey;

        var langConf = new ConfigEntity<List<string>>(textManifest);
        
        var lang = FindBestMatchLanguage(langConf.Data);
        var langFile = Path.Combine(textFolder, $"{lang}.lang");
        
        var langs = File.ReadAllLines(langFile);
        return langs.First(t => t.Contains(langKey))
            .Replace("#", "")
            .Split('=')[1]
            .Replace("\\n","\n");
    }
    
    private static string FindBestMatchLanguage(List<string> supportedLanguages)
    {
        // 1. 获取当前系统的语言和区域信息
        CultureInfo currentCulture = CultureInfo.CurrentUICulture; // 或者 CultureInfo.CurrentCulture
        
        // 2. 获取语言代码（不带区域）
        string currentLanguage = currentCulture.TwoLetterISOLanguageName.ToLower();
        string currentFullLocale = currentCulture.Name; // 例如 "zh-CN"
        
        Console.WriteLine($"当前系统语言: {currentCulture.DisplayName}");
        Console.WriteLine($"语言代码: {currentLanguage}, 完整区域: {currentFullLocale}");
        
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