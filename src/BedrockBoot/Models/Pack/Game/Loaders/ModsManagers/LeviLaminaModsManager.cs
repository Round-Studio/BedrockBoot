using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;

public class LeviLaminaModsManager : IModsManager
{
    private VersionConfig _versionConfig;

    public void Init(VersionConfig instance)
    {
        _versionConfig = instance;
    }

    public Action? OnRefresh { get; set; }

    public List<ModItemInfo> GetAllMods()
    {
        var modsFolder = Path.Combine(_versionConfig.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods");
        var result = new List<ModItemInfo>();
        Directory.GetDirectories(modsFolder).ToList().ForEach(folder =>
        {
            var manifestFile = Path.Combine(folder, "manifest.json");
            var conf = new ConfigEntity<LocalManifest>(manifestFile, false).Data;
            if (conf.Name != "LeviLamina")
            {
                result.Add(new()
                {
                    ModPath = Path.Combine(folder, conf.Entry),
                    Version = conf.Version,
                    ModInjectType = ModType.Native,
                    ModName = conf.Name,
                    ModLoaderType = typeof(LoaderInstance.LeviLamina),
                    ModDescription = conf.Description
                });
            }
        });

        return result;
    }

    public async Task AddMod()
    {
        var topLevel = TopLevel.GetTopLevel(GlobalModel.MainWindow);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 ZIP 文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("ZIP 文件")
                {
                    Patterns = new[] { "*.zip" },
                    MimeTypes = new[] { "application/zip" }
                }
            }
        });

        var file = files?.FirstOrDefault()?.TryGetLocalPath();
        ZipHelper.ExtractZipFile(file,
            Path.Combine(_versionConfig.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods"), true);
        OnRefresh?.Invoke();
    }

    public void Remove(ModItemInfo info)
    {
        var folder = Path.GetDirectoryName(info.ModPath);
        Directory.Delete(folder!, true);
        OnRefresh?.Invoke();
    }
}

public class LocalManifest
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("entry")] public string Entry { get; set; } = string.Empty;

    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("platform")] public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")] public string Author { get; set; } = string.Empty;
}