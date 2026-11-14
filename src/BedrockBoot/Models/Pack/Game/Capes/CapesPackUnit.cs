using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Capes;

public class CapesPackUnit
{
    public ConfigEntity<CapePackManifest>? SourceManifest { get; set; }
    public ConfigEntity<CapeIndex>? CapeIndexManifest { get; set; }
    public VersionConfig? Instance { get; set; }
    public string Uuid => SourceManifest?.Data.Header.Uuid!;
    public string? Folder { get; set; }
    public string? PackIcon => Path.Combine(Folder!, "pack_icon.png");
    public void Delete() => Directory.Delete(Folder!, true);

    public List<CapeDefinition> LoadCapeFile()
    {
        CapeIndexManifest = new ConfigEntity<CapeIndex>(Path.Combine(Folder!, "cape.json"));
        return CapeIndexManifest?.Data.Capes ?? new List<CapeDefinition>();
    }

    public void AddCape(CapeFileInfo info)
    {
        var uid = Guid.NewGuid().ToString("N").Substring(0, 8);
        var fileName = $"{uid}_{Path.GetFileName(info.CapeImagePath)}";
        var locCapeFile = Path.Combine(Folder, "textures", fileName);
        var locCapeIconFile = Path.Combine(Folder, "textures", $"icon_{fileName}");
        var locCapeElytraFile = Path.Combine(Folder, "textures", $"elytra_{fileName}");
        
        File.WriteAllBytes(locCapeFile, File.ReadAllBytes(info.CapeImagePath));
        CapeImageHelper.PatchCapeIcon(locCapeFile, locCapeIconFile);
        CapeImageHelper.PatchCapeElytra(locCapeFile, locCapeElytraFile);
        
        CapeIndexManifest?.Data.Capes.Add(new CapeDefinition()
        {
            Elytra = $"textures/elytra_{fileName}",
            Icon = $"textures/icon_{fileName}",
            Texture = $"textures/{fileName}",
            Rarity = info.Type.ToString().ToLower(),
            LocalizationName = $"cape.{CapeIndexManifest.Data.LocalizationName}.{uid}"
        });
        CapeIndexManifest?.Save();
    }
    
    public void ExportPack(string savePath)
    {
        if (string.IsNullOrWhiteSpace(Folder) || !Directory.Exists(Folder))
            throw new DirectoryNotFoundException($"披风包目录不存在: {Folder}");

        if (File.Exists(savePath))
            File.Delete(savePath);

        var targetDir = Path.GetDirectoryName(Path.GetFullPath(savePath));
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        ZipFile.CreateFromDirectory(
            Folder,
            savePath,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);
    }

    public void SetPackIcon(string icon)
    {
        File.WriteAllBytes(PackIcon!, File.ReadAllBytes(icon));
    }
}