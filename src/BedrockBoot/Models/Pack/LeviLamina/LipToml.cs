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
using Tomlyn;
using Tomlyn.Model;

namespace BedrockBoot.Models.Pack.LeviLamina;

/// <summary>
/// lip.toml（LiteLDev 新一代清单，format = 4）的解析与到 tooth.json 模型（LeviConfig）的映射。
/// 与旧 tooth.json 的差异：
///   tooth → id；variants 数组 → variants."&lt;label&gt;" 表数组（label 为键，新清单用 side 区分 client/server）；
///   dependencies → deps；placements → place（不再有 type 字段，src 以 / 结尾表示目录）；
///   remove_files → purge；scripts → hooks；URL 占位符 {{tooth}} → {{id}}。
/// </summary>
public static class LipToml
{
    /// <summary>解析 lip.toml 并映射为 LeviConfig（tooth.json 兼容模型），失败返回 null。
    /// 对 JSON 文本（旧 tooth.json）TOML 解析必然失败返回 null，调用方以此就地回落 JSON 反序列化。</summary>
    public static LeviConfig Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // JSON 文本（旧 tooth.json）直接交给调用方的 JSON 反序列化，不作为 TOML 解析
        if (text.TrimStart().StartsWith("{")) return null;

        try
        {
            var doc = Toml.Parse(text);
            if (doc.HasErrors) return null;

            var root = doc.ToModel();

            var config = new LeviConfig
            {
                FormatVersion = root.TryGetValue("format", out var format) && format is long f ? (int)f : 4,
                Tooth = root.TryGetValue("id", out var id) ? id as string : null,
                Version = root.TryGetValue("version", out var version) ? version as string : null
            };

            if (root.TryGetValue("info", out var infoRaw) && infoRaw is TomlTable info)
            {
                config.Info = new Info
                {
                    Name = info.TryGetValue("name", out var name) ? name as string : null,
                    Description = info.TryGetValue("description", out var description) ? description as string : null,
                    AvatarUrl = info.TryGetValue("logo", out var logo) ? logo as string : null
                };

                if (info.TryGetValue("tags", out var tagsRaw) && tagsRaw is TomlArray tags)
                    config.Info.Tags = tags.OfType<string>().ToList();
            }

            if (root.TryGetValue("variants", out var variantsRaw) && variantsRaw is TomlTable variants)
                config.Variants = ParseVariants(variants);

            if (config.Variants == null || config.Variants.Count == 0) return null;
            return config;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<Variant> ParseVariants(TomlTable variants)
    {
        var result = new List<Variant>();

        foreach (var label in variants.Keys)
        {
            if (variants[label] is not TomlTableArray entries) continue;

            foreach (var entry in entries.OfType<TomlTable>())
            {
                var variant = new Variant
                {
                    // 旧模型里 Label 是 variants 数组项的自带字段；新清单 label 是外层键
                    Label = label,
                    // 新 lip.toml：side = "client"/"server"，替代旧的 label = "client" 约定
                    Side = entry.TryGetValue("side", out var side) ? side as string : null,
                    Platform = entry.TryGetValue("platform", out var platform) ? platform as string : null
                };

                if (entry.TryGetValue("deps", out var depsRaw) && depsRaw is TomlTable deps)
                {
                    variant.Dependencies = new Dictionary<string, string>();
                    foreach (var dep in deps)
                        variant.Dependencies[dep.Key] = dep.Value?.ToString();
                }

                if (entry.TryGetValue("assets", out var assetsRaw) && assetsRaw is TomlTableArray assets)
                {
                    variant.Assets = new List<Asset>();
                    foreach (var assetTable in assets.OfType<TomlTable>())
                    {
                        var asset = new Asset
                        {
                            Type = assetTable.TryGetValue("type", out var type) ? type as string : "zip",
                            Urls = new List<string>(),
                            Placements = new List<Placement>()
                        };

                        if (assetTable.TryGetValue("urls", out var urlsRaw) && urlsRaw is TomlArray urls)
                            asset.Urls.AddRange(urls.OfType<string>());

                        if (assetTable.TryGetValue("place", out var placeRaw) && placeRaw is TomlArray places)
                            foreach (var placeTable in places.OfType<TomlTable>())
                            {
                                var src = placeTable.TryGetValue("src", out var srcRaw) ? srcRaw as string : null;
                                var dest = placeTable.TryGetValue("dest", out var destRaw) ? destRaw as string : null;
                                if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dest)) continue;

                                // place 无 type 字段：src 以 / 结尾视为目录放置
                                asset.Placements.Add(new Placement
                                {
                                    Type = src.EndsWith("/") ? "dir" : "file",
                                    Src = src,
                                    Dest = dest
                                });
                            }

                        variant.Assets.Add(asset);
                    }
                }

                if (entry.TryGetValue("purge", out var purgeRaw) && purgeRaw is TomlArray purge)
                    variant.RemoveFiles = purge.OfType<string>().ToList();

                if (entry.TryGetValue("hooks", out var hooksRaw) && hooksRaw is TomlTable hooks)
                {
                    variant.Scripts = new Scripts();
                    if (hooks.TryGetValue("post_install", out var postInstallRaw) &&
                        postInstallRaw is TomlArray postInstall)
                        variant.Scripts.PostInstall = postInstall.OfType<string>().ToList();
                    if (hooks.TryGetValue("post_uninstall", out var postUninstallRaw) &&
                        postUninstallRaw is TomlArray postUninstall)
                        variant.Scripts.PostUninstall = postUninstall.OfType<string>().ToList();
                }

                result.Add(variant);
            }
        }

        return result;
    }
}
