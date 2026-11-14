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
using System.IO.Compression;
using System.Linq;
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
        CapeIndexManifest ??= new ConfigEntity<CapeIndex>(Path.Combine(Folder!, "cape.json"));
        return CapeIndexManifest.Data.Capes ?? new List<CapeDefinition>();
    }

    /// <summary>组合语言键（对齐 PreLoad/BLoader 读取约定）：稳定键 → cape.&lt;包键&gt;.&lt;稳定键&gt;；
    /// 旧格式（已含包键前缀）原样返回。</summary>
    public string BuildLangKey(string stableKey)
    {
        var packKey = CapeIndexManifest?.Data.LocalizationName;
        if (string.IsNullOrEmpty(packKey) || stableKey.StartsWith($"cape.{packKey}.")) return stableKey;
        return $"cape.{packKey}.{stableKey}";
    }

    /// <summary>语言行是否为指定键（key=value 精确匹配键名）。</summary>
    private static bool IsLangLine(string line, string key)
    {
        var separator = line.IndexOf('=');
        return separator > 0 && line[..separator].Trim() == key;
    }

    public void RemoveCape(CapeDefinition cape)
    {
        CapeIndexManifest ??= new ConfigEntity<CapeIndex>(Path.Combine(Folder!, "cape.json"));

        foreach (var path in new[] { cape.Texture, cape.Icon, cape.Elytra })
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            var file = Path.Combine(Folder!, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file)) File.Delete(file);
        }

        var langKey = BuildLangKey(cape.LocalizationName);
        foreach (var locale in new[] { "zh_CN", "en_US" })
        {
            var langFile = Path.Combine(Folder!, "texts", $"{locale}.lang");
            if (!File.Exists(langFile)) continue;

            var lines = File.ReadAllLines(langFile)
                .Where(line => !IsLangLine(line, langKey) && !IsLangLine(line, cape.LocalizationName))
                .ToList();
            File.WriteAllLines(langFile, lines);
        }

        CapeIndexManifest.Data.Capes.RemoveAll(c => c.LocalizationName == cape.LocalizationName);
        CapeIndexManifest.Save();
    }

    public void AddCape(CapeFileInfo info)
    {
        var uid = Guid.NewGuid().ToString("N").Substring(0, 8);
        var fileName = $"{uid}_{Path.GetFileName(info.CapeImagePath)}";
        var locCapeFile = Path.Combine(Folder!, "textures", fileName);
        var locCapeIconFile = Path.Combine(Folder!, "textures", $"icon_{fileName}");
        var locCapeElytraFile = Path.Combine(Folder!, "textures", $"elytra_{fileName}");

        if (!Directory.Exists(Path.Combine(Folder!, "textures")))
            Directory.CreateDirectory(Path.Combine(Folder!, "textures"));

        File.WriteAllBytes(locCapeFile, File.ReadAllBytes(info.CapeImagePath));
        CapeImageHelper.PatchCapeIcon(locCapeFile, locCapeIconFile);
        CapeImageHelper.PatchCapeElytra(locCapeFile, locCapeElytraFile);

        // localization_name 只存稳定键（uid）；PreLoad 读取时会按 cape.<包键>.<稳定键> 查显示名。
        var packKey = CapeIndexManifest!.Data.LocalizationName;
        AddLanguage($"cape.{packKey}.{uid}", info.CapeName);
        CapeIndexManifest?.Data.Capes.Add(new CapeDefinition()
        {
            Elytra = $"textures/elytra_{fileName}",
            Icon = $"textures/icon_{fileName}",
            Texture = $"textures/{fileName}",
            Rarity = info.Type.ToString().ToLower(),
            LocalizationName = uid
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

    public void AddLanguage(string key, string content)
    {
        var langFile = Path.Combine(Folder!, "texts", "zh_CN.lang");
        if (!File.Exists(langFile)) return;

        var lines = File.ReadAllLines(langFile).ToList();
        var index = lines.FindIndex(line => IsLangLine(line, key));
        if (index >= 0)
            lines[index] = $"{key}={content}";
        else
            lines.Add($"{key}={content}");
        File.WriteAllLines(langFile, lines);
    }

    /// <summary>按语言键精确查找值（不做组合），zh_CN 优先、en_US 兜底；找不到返回 null。</summary>
    private string? GetLangValue(string key)
    {
        foreach (var locale in new[] { "zh_CN", "en_US" })
        {
            var langFile = Path.Combine(Folder!, "texts", $"{locale}.lang");
            if (!File.Exists(langFile)) continue;

            foreach (var line in File.ReadAllLines(langFile))
            {
                if (!IsLangLine(line, key)) continue;

                var value = line[(line.IndexOf('=') + 1)..].Trim();
                if (value.Length > 0) return value;
            }
        }

        return null;
    }

    /// <summary>包显示名：语言文件 capepack.&lt;原包名&gt; 优先，回退 manifest 包名。</summary>
    public string PackDisplayName()
    {
        LoadCapeFile();
        var packKey = CapeIndexManifest?.Data.LocalizationName;
        if (!string.IsNullOrEmpty(packKey))
        {
            var display = GetLangValue($"capepack.{packKey}");
            if (!string.IsNullOrEmpty(display)) return display;
        }

        return SourceManifest?.Data.Header.Name ?? "Local Cape Pack";
    }

    /// <summary>修改包名：manifest 包名即时更新；显示名写入语言文件，键固定为
    /// capepack.&lt;原包名&gt;（cape.json 的 localization_name，旧包缺省时以首次修改前的包名固化）。</summary>
    public void SetPackName(string name)
    {
        var oldName = SourceManifest!.Data.Header.Name;
        SourceManifest.Data.Header.Name = name;
        SourceManifest.Save();

        LoadCapeFile();
        var packKey = CapeIndexManifest!.Data.LocalizationName;
        if (string.IsNullOrEmpty(packKey))
        {
            packKey = oldName;
            CapeIndexManifest.Data.LocalizationName = packKey;
            CapeIndexManifest.Save();
        }

        AddLanguage($"capepack.{packKey}", name);
    }

    public string GetLanguage(string key)
    {
        // 对齐 PreLoad 读取约定：显示名键为 cape.<包键>.<稳定键>；旧格式（已含完整键）原样匹配。
        var langKey = BuildLangKey(key);
        return GetLangValue(langKey) ?? GetLangValue(key) ?? "Cape";
    }
}