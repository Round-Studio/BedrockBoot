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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Interface.ModLoader;
using ICSharpCode.SharpZipLib.Zip;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;

public class LeviLaminaModsManager : IModsManager
{
    public bool IsModCanEnable { get; set; } = false;
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
            Title = I18nManager.Instance["FilePicker.ChooseZip"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(I18nManager.Instance["FileType.Zip"])
                {
                    Patterns = new[] { "*.zip" },
                    MimeTypes = new[] { "application/zip" }
                }
            }
        });

        var file = files?.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrEmpty(file)) return;

        var modsFolder = Path.Combine(_versionConfig.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods");

        // 包内没自带一层文件夹时先建目录再解压，避免文件直接泼进 ll.mods 根目录
        ZipHelper.ExtractZipFile(file, ResolveExtractTarget(file, modsFolder), true);
        OnRefresh?.Invoke();
    }

    /// <summary>
    ///     决定模组包的解压目标。zip 内容已收在唯一一个顶层目录里时原地解压；
    ///     否则（根目录散放文件/多个顶层目录）按包内 manifest.json 的 name（其次 zip 文件名）建目录再解压，
    ///     保证 ll.mods 下仍是「一个模组一个文件夹」的布局（GetAllMods 按 manifest.json 扫描）。
    /// </summary>
    private static string ResolveExtractTarget(string zipPath, string modsFolder)
    {
        try
        {
            using var zip = new ZipFile(zipPath);

            var topSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasRootFile = false;
            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory) continue;

                var name = entry.Name.Replace('\\', '/').TrimStart('/');
                var slashIndex = name.IndexOf('/');
                if (slashIndex < 0)
                    hasRootFile = true;
                else
                    topSegments.Add(name.Substring(0, slashIndex));
            }

            if (!hasRootFile && topSegments.Count == 1)
                return modsFolder;

            var folderName = ReadRootManifestName(zip)
                             ?? SanitizeFolderName(Path.GetFileNameWithoutExtension(zipPath));

            return Path.Combine(modsFolder, folderName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析模组包结构失败，按原目录解压: {ex.Message}");
            return modsFolder;
        }
    }

    private static string ReadRootManifestName(ZipFile zip)
    {
        var entry = zip.Cast<ZipEntry>()
            .FirstOrDefault(e => !e.IsDirectory && string.Equals(
                e.Name.Replace('\\', '/').TrimStart('/'), "manifest.json", StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;

        try
        {
            using var stream = zip.GetInputStream(entry);
            using var reader = new StreamReader(stream);
            var manifest = JsonSerializer.Deserialize<LocalManifest>(reader.ReadToEnd());
            return string.IsNullOrWhiteSpace(manifest?.Name) ? null : SanitizeFolderName(manifest.Name);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFolderName(string name)
    {
        var clean = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
        return string.IsNullOrEmpty(clean) ? "ImportedMod" : clean;
    }

    public void Remove(ModItemInfo info)
    {
        var folder = Path.GetDirectoryName(info.ModPath);
        Directory.Delete(folder!, true);
        OnRefresh?.Invoke();
    }

    public void SetEnable(ModItemInfo info) =>
        throw new NotImplementedException();
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