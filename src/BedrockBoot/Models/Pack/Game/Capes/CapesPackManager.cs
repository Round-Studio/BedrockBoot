using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace BedrockBoot.Models.Pack.Game.Capes;

public class CapesPackInfo
{
    public string PackPath { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string PackUuid { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public List<string> CapeKeys { get; set; } = new();
}

public class CapesPackManager
{
    public static string GetPacksRoot(string versionPath) =>
        Path.Combine(versionPath, "config", "BedrockBoot2", "capes");

    public static string GetPackPath(string versionPath, string folderName) =>
        Path.Combine(GetPacksRoot(versionPath), folderName);

    public static List<CapesPackInfo> LoadPacks(string versionPath)
    {
        var packs = new List<CapesPackInfo>();
        var root = GetPacksRoot(versionPath);
        if (!Directory.Exists(root)) return packs;

        foreach (var dir in Directory.EnumerateDirectories(root).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var manifestPath = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifestPath)) continue;
                if (JsonNode.Parse(File.ReadAllText(manifestPath)) is not JsonObject manifest) continue;

                var header = manifest["header"] as JsonObject;
                var authors = new List<string>();
                if (manifest["metadata"] is JsonObject metadata)
                {
                    if (metadata["authors"] is JsonArray authorsArray)
                    {
                        authors.AddRange(authorsArray
                            .Where(a => a is JsonValue)
                            .Select(a => a!.GetValue<string>()));
                    }
                    else if (metadata["author"] is JsonValue singleAuthor)
                    {
                        authors.Add(singleAuthor.GetValue<string>());
                    }
                }

                packs.Add(new CapesPackInfo
                {
                    PackPath = dir,
                    FolderName = Path.GetFileName(dir),
                    PackName = header?["name"]?.GetValue<string>() ?? Path.GetFileName(dir),
                    PackUuid = header?["uuid"]?.GetValue<string>() ?? string.Empty,
                    Authors = string.Join(", ", authors),
                    IconPath = File.Exists(Path.Combine(dir, "pack_icon.png"))
                        ? Path.Combine(dir, "pack_icon.png")
                        : string.Empty,
                    CapeKeys = LoadCapeList(versionPath, Path.GetFileName(dir)),
                });
            }
            catch
            {
            }
        }

        return packs;
    }

    public static CapesPackInfo? CreatePack(string versionPath, string packName, string author, byte[]? iconBytes)
    {
        try
        {
            var root = GetPacksRoot(versionPath);
            Directory.CreateDirectory(root);

            var folderName = MakeSafeName(packName);
            var packPath = Path.Combine(root, folderName);
            var suffix = 2;
            while (Directory.Exists(packPath))
            {
                packPath = Path.Combine(root, $"{folderName}_{suffix}");
                suffix++;
            }
            Directory.CreateDirectory(packPath);

            var packUuid = Guid.NewGuid().ToString();
            var moduleUuid = Guid.NewGuid().ToString();

            var manifest = new JsonObject
            {
                ["format_version"] = 1,
                ["header"] = new JsonObject
                {
                    ["name"] = packName,
                    ["uuid"] = packUuid,
                    ["version"] = new JsonArray { 1, 0, 0 },
                },
                ["modules"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "cape_pack",
                        ["uuid"] = moduleUuid,
                        ["version"] = new JsonArray { 1, 0, 0 },
                    },
                },
            };
            if (!string.IsNullOrWhiteSpace(author))
            {
                manifest["metadata"] = new JsonObject
                {
                    ["authors"] = new JsonArray { author },
                };
            }

            File.WriteAllText(Path.Combine(packPath, "manifest.json"),
                manifest.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(packPath, "cape.json"), "{}");

            if (iconBytes is { Length: > 0 })
            {
                File.WriteAllBytes(Path.Combine(packPath, "pack_icon.png"), iconBytes);
            }

            return new CapesPackInfo
            {
                PackPath = packPath,
                FolderName = Path.GetFileName(packPath),
                PackName = packName,
                PackUuid = packUuid,
                Authors = author,
                IconPath = iconBytes is { Length: > 0 } ? Path.Combine(packPath, "pack_icon.png") : string.Empty,
            };
        }
        catch
        {
            return null;
        }
    }

    public static bool DeletePack(string versionPath, string folderName)
    {
        try
        {
            var packPath = GetPackPath(versionPath, folderName);
            if (!Directory.Exists(packPath)) return false;
            Directory.Delete(packPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool UpdatePackName(string versionPath, string folderName, string newName)
    {
        try
        {
            var manifestPath = Path.Combine(GetPackPath(versionPath, folderName), "manifest.json");
            if (!File.Exists(manifestPath)) return false;
            if (JsonNode.Parse(File.ReadAllText(manifestPath)) is not JsonObject manifest) return false;
            if (manifest["header"] is not JsonObject header) return false;

            header["name"] = newName;
            File.WriteAllText(manifestPath,
                manifest.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetAuthors(string versionPath, string folderName, string author)
    {
        try
        {
            var manifestPath = Path.Combine(GetPackPath(versionPath, folderName), "manifest.json");
            if (!File.Exists(manifestPath)) return false;
            if (JsonNode.Parse(File.ReadAllText(manifestPath)) is not JsonObject manifest) return false;

            if (string.IsNullOrWhiteSpace(author))
            {
                if (manifest["metadata"] is JsonObject metadata)
                {
                    metadata.Remove("authors");
                    metadata.Remove("author");
                    if (metadata.Count == 0) manifest.Remove("metadata");
                }
            }
            else
            {
                manifest["metadata"] = new JsonObject
                {
                    ["authors"] = new JsonArray { author },
                };
            }

            File.WriteAllText(manifestPath,
                manifest.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetIcon(string versionPath, string folderName, byte[] iconBytes)
    {
        try
        {
            var packPath = GetPackPath(versionPath, folderName);
            if (!Directory.Exists(packPath)) return false;
            File.WriteAllBytes(Path.Combine(packPath, "pack_icon.png"), iconBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static List<string> LoadCapeList(string versionPath, string folderName)
    {
        var names = new List<string>();
        try
        {
            var capeJsonPath = Path.Combine(GetPackPath(versionPath, folderName), "cape.json");
            if (!File.Exists(capeJsonPath)) return names;
            if (JsonNode.Parse(File.ReadAllText(capeJsonPath)) is not JsonObject sheet) return names;
            if (sheet["capes"] is JsonArray capes)
            {
                names.AddRange(capes.OfType<JsonObject>()
                    .Select(c => c["localization_name"]?.GetValue<string>() ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n)));
            }
        }
        catch
        {
        }
        return names;
    }

    public static string? AddCape(string versionPath, string folderName, string sourcePngPath)
    {
        try
        {
            var packPath = GetPackPath(versionPath, folderName);
            if (!Directory.Exists(packPath)) return "披风包不存在";

            var bytes = File.ReadAllBytes(sourcePngPath);
            if (!IsPng(bytes)) return "所选文件不是 PNG 图片";
            if (bytes.Length > 8 * 1024 * 1024) return "图片超过 8 MiB 上限";
            if (!TryPngDimensions(bytes, out var width, out var height)) return "PNG 文件无法解析";
            if (width % 64 != 0 || height % 32 != 0 || width / 64 != height / 32)
            {
                return $"纹理尺寸 {width}x{height} 不符合 64x32 整数倍等比要求（如 64x32、128x64、256x128）";
            }

            var texturesDir = Path.Combine(packPath, "textures");
            Directory.CreateDirectory(texturesDir);

            var stem = Path.GetFileNameWithoutExtension(sourcePngPath).ToLowerInvariant();
            var fileName = $"{MakeSafeName(stem)}.png";
            var targetPath = Path.Combine(texturesDir, fileName);
            var suffix = 2;
            while (File.Exists(targetPath))
            {
                targetPath = Path.Combine(texturesDir, $"{MakeSafeName(stem)}_{suffix}.png");
                suffix++;
            }
            File.WriteAllBytes(targetPath, bytes);

            var stableKey = Path.GetFileNameWithoutExtension(targetPath);
            var capeJsonPath = Path.Combine(packPath, "cape.json");
            if (JsonNode.Parse(File.ReadAllText(capeJsonPath)) is not JsonObject sheet)
            {
                sheet = new JsonObject();
            }
            if (sheet["capes"] is not JsonArray capes)
            {
                capes = new JsonArray();
                sheet["capes"] = capes;
            }

            foreach (var cape in capes.OfType<JsonObject>())
            {
                if (cape["localization_name"]?.GetValue<string>() == stableKey)
                {
                    return $"披风 {stableKey} 已存在";
                }
            }

            capes.Add(new JsonObject
            {
                ["localization_name"] = stableKey,
                ["texture"] = $"textures/{Path.GetFileName(targetPath)}",
            });

            if (sheet["localization_name"] is not JsonValue)
            {
                sheet["localization_name"] = "capepack";
            }

            var packKey = sheet["localization_name"]!.GetValue<string>();
            var textsDir = Path.Combine(packPath, "texts");
            Directory.CreateDirectory(textsDir);
            var langPath = Path.Combine(textsDir, "zh_CN.lang");
            var lines = File.Exists(langPath)
                ? File.ReadAllLines(langPath).ToList()
                : new List<string>();
            var capeKey = $"cape.{packKey}.{stableKey}";
            if (!lines.Any(l => l.TrimStart().StartsWith(capeKey + "=", StringComparison.Ordinal)))
            {
                lines.Add($"{capeKey}={stableKey}");
                File.WriteAllLines(langPath, lines, global::System.Text.Encoding.UTF8);
            }

            File.WriteAllText(capeJsonPath,
                sheet.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }

    public static bool RemoveCape(string versionPath, string folderName, string stableKey)
    {
        try
        {
            var packPath = GetPackPath(versionPath, folderName);
            var capeJsonPath = Path.Combine(packPath, "cape.json");
            if (!File.Exists(capeJsonPath)) return false;
            if (JsonNode.Parse(File.ReadAllText(capeJsonPath)) is not JsonObject sheet) return false;
            if (sheet["capes"] is not JsonArray capes) return false;

            JsonObject? target = null;
            foreach (var cape in capes.OfType<JsonObject>())
            {
                if (cape["localization_name"]?.GetValue<string>() == stableKey)
                {
                    target = cape;
                    break;
                }
            }
            if (target is null) return false;

            var texture = target["texture"]?.GetValue<string>();
            capes.Remove(target);
            if (capes.Count == 0) sheet.Remove("capes");

            File.WriteAllText(capeJsonPath,
                sheet.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            if (!string.IsNullOrEmpty(texture))
            {
                var texturePath = Path.Combine(packPath, texture.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(texturePath)) File.Delete(texturePath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? GetTexturePath(string versionPath, string folderName, string stableKey)
    {
        try
        {
            var capeJsonPath = Path.Combine(GetPackPath(versionPath, folderName), "cape.json");
            if (!File.Exists(capeJsonPath)) return null;
            if (JsonNode.Parse(File.ReadAllText(capeJsonPath)) is not JsonObject sheet) return null;
            if (sheet["capes"] is not JsonArray capes) return null;

            foreach (var cape in capes.OfType<JsonObject>())
            {
                if (cape["localization_name"]?.GetValue<string>() == stableKey)
                {
                    var texture = cape["texture"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(texture)) return null;
                    return Path.Combine(GetPackPath(versionPath, folderName),
                        texture.Replace('/', Path.DirectorySeparatorChar));
                }
            }
        }
        catch
        {
        }
        return null;
    }

    public static string? ExportPack(string versionPath, string folderName, string targetZipPath)
    {
        try
        {
            var packPath = GetPackPath(versionPath, folderName);
            if (!Directory.Exists(packPath)) return "披风包不存在";

            if (File.Exists(targetZipPath)) File.Delete(targetZipPath);
            ZipFile.CreateFromDirectory(packPath, targetZipPath, CompressionLevel.Optimal, false);
            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }

    private static string MakeSafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Where(c => !invalid.Contains(c) && c != ' ').ToArray()).Trim();
        if (safe.Length == 0) safe = "cape";
        return safe.Length > 64 ? safe[..64] : safe;
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    private static bool TryPngDimensions(byte[] bytes, out int width, out int height)
    {
        width = height = 0;
        if (bytes.Length < 24 || !IsPng(bytes)) return false;
        width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return width > 0 && height > 0;
    }
}
