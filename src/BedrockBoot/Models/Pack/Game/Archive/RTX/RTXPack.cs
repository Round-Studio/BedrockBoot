using System.IO;
using System.Text.Json;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack;
using BedrockBoot.Standard.Enum;

namespace BedrockBoot.Models.Pack.Game.Archive.RTX;

public class RTXPack
{
    public static ResourcePackManifest RTXPackManifest => new()
    {
        FormatVersion = 3,
        PackType = ResourcePackType.Resource,
        Header = new()
        {
            Description = "BedrockBoot RTX Tools",
            Name = "BedrockBoot RTX Tools",
            Uuid = "39130818-56a5-42d5-8388-47d232f3ef14",
            VersionElement = JsonDocument.Parse("\"1, 0, 0\"").RootElement,
            MinEngineVersionElement = JsonDocument.Parse("\"1, 13, 0\"").RootElement,
        },
        Modules = new()
        {
            new()
            {
                Description = "BedrockBoot RTX",
                Type = "resources",
                Uuid = "c6d25803-71be-4363-88de-3b1f8a22812a",
                VersionElement = JsonDocument.Parse("\"1, 0, 0\"").RootElement,
            }
        },
        Capabilities = new()
        {
            "pbr",
            "raytraced"
        },
        Metadata = new()
        {
            Authors = new()
            {
                "Dime12022"
            }
        }
    };

    public static void ExportPack(VersionConfig instance)
    {
        var exportPath = Path.Combine(instance.VersionPath!, "data", "rtx_pack");
        var rtxManifest = RTXPackManifest;
        rtxManifest.PackRootPath = exportPath;
        rtxManifest.SaveConfig();
    }

    public static void InstallPack(ArchiveInfo info)
    {
        var rcp = Path.Combine(info.Path, "resource_packs");
        var rtxPackPath = Path.Combine(rcp, RTXPackManifest.Header.Uuid);
        if (!Directory.Exists(rcp))
            Directory.CreateDirectory(rcp);

        if (!Directory.Exists(rtxPackPath))
            Directory.CreateSymbolicLink(rtxPackPath, Path.Combine(info.VersionInfo.VersionPath!, "data", "rtx_pack"));
    }

    public static void RemovePack(ArchiveInfo info)
    {
        var rcp = Path.Combine(info.Path, "resource_packs");
        var rtxPackPath = Path.Combine(rcp, RTXPackManifest.Header.Uuid);
        if (!Directory.Exists(rcp))
            Directory.CreateDirectory(rcp);

        if (Directory.Exists(rtxPackPath))
            Directory.Delete(rtxPackPath);
    }
}