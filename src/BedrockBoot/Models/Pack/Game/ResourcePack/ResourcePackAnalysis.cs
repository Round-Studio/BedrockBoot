using System;
using System.Collections.Generic;
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
            var conf = new ConfigEntity<ResourcePackManifest>(manifestFile).Data;
            conf.PackRootPath = _tempPath;
            conf.PackType = type;
            
            result.Add(conf);
        }
        else if(type == ResourcePackType.Addon)
        {
            var folder = Directory.GetDirectories(_tempPath);
            var files = folder.Select(f => Path.Combine(f, "manifest.json")).ToList();
            
            files.ForEach(f =>
            {
                var conf = new ConfigEntity<ResourcePackManifest>(f).Data;
                conf.PackRootPath = Path.GetDirectoryName(f);
                conf.PackType = GetPackType(conf);
            
                result.Add(conf);
            });
        }
        else
        {
            return null;
        }

        return result;
    }
}