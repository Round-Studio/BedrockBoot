/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
using BedrockBoot.Interface.ModLoader;
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