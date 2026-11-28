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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Pack.LeviLamina
{
    // ==== levimc.org API v4 数据模型 ====
    // 索引：https://levimc.org/api/v4/mods/index.json
    // 详情：https://levimc.org/api/v4/mods/{id}/index.json（id 大小写敏感，形如 github.com/LiteLDev/Arke）

    public class LeviModsIndexResponse
    {
        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("stats")] public LeviModsIndexStats Stats { get; set; }

        [JsonPropertyName("sort")] public string Sort { get; set; }

        [JsonPropertyName("mods")] public List<LeviModEntry> Mods { get; set; }
    }

    public class LeviModsIndexStats
    {
        [JsonPropertyName("projects")] public int Projects { get; set; }

        [JsonPropertyName("authors")] public int Authors { get; set; }

        [JsonPropertyName("versions")] public int Versions { get; set; }
    }

    public class LeviModEntry
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("owner")] public string Owner { get; set; }

        [JsonPropertyName("logo")] public string Logo { get; set; }

        [JsonPropertyName("tags")] public List<string> Tags { get; set; }

        [JsonPropertyName("side")] public List<string> Side { get; set; }

        [JsonPropertyName("platform")] public List<string> Platform { get; set; }

        [JsonPropertyName("gameVersions")] public List<string> GameVersions { get; set; }

        [JsonPropertyName("deps")] public Dictionary<string, string> Deps { get; set; }

        [JsonPropertyName("stars")] public int Stars { get; set; }

        [JsonPropertyName("downloads")] public int Downloads { get; set; }

        [JsonPropertyName("updated")] public DateTime Updated { get; set; }

        [JsonPropertyName("created")] public DateTime Created { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("json")] public string Json { get; set; }
    }

    public class LeviModDetail : LeviModEntry
    {
        [JsonPropertyName("repository")] public string Repository { get; set; }

        [JsonPropertyName("issues")] public string Issues { get; set; }

        [JsonPropertyName("releases")] public string Releases { get; set; }

        [JsonPropertyName("license")] public string License { get; set; }

        [JsonPropertyName("published")] public DateTime Published { get; set; }

        [JsonPropertyName("latest")] public LeviLatestInfo Latest { get; set; }

        [JsonPropertyName("readmes")] public List<LeviReadme> Readmes { get; set; }

        [JsonPropertyName("versions")] public List<LeviModVersion> Versions { get; set; }
    }

    public class LeviLatestInfo
    {
        [JsonPropertyName("variant")] public string Variant { get; set; }

        [JsonPropertyName("version")] public string Version { get; set; }

        [JsonPropertyName("install")] public string Install { get; set; }
    }

    public class LeviReadme
    {
        [JsonPropertyName("file")] public string File { get; set; }

        [JsonPropertyName("lang")] public string Lang { get; set; }

        [JsonPropertyName("text")] public string Text { get; set; }
    }

    public class LeviModVersion
    {
        [JsonPropertyName("version")] public string Version { get; set; }

        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }

        [JsonPropertyName("released")] public DateTime Released { get; set; }

        [JsonPropertyName("releaseUrl")] public string ReleaseUrl { get; set; }

        [JsonPropertyName("manifestUrl")] public string ManifestUrl { get; set; }

        [JsonPropertyName("info")] public LeviModVersionInfo Info { get; set; }

        [JsonPropertyName("variants")] public Dictionary<string, List<LeviVariantAsset>> Variants { get; set; }
    }

    public class LeviModVersionInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("tags")] public List<string> Tags { get; set; }
    }

    public class LeviVariantAsset
    {
        [JsonPropertyName("side")] public string Side { get; set; }

        [JsonPropertyName("platform")] public string Platform { get; set; }

        [JsonPropertyName("deps")] public Dictionary<string, string> Deps { get; set; }
    }

    /// <summary>
    ///     levimc.org API v4 数据源。搜索走全局索引，详情/版本/依赖走模组子接口，均带进程内缓存。
    ///     替代已停维的 lipr.levimc.org 注册表。
    /// </summary>
    public static class LeviModsSource
    {
        private const string ApiBase = "https://levimc.org/api/v4/mods";

        private static readonly HttpClient HttpClient = CreateClient();
        private static readonly SemaphoreSlim IndexLock = new(1, 1);
        private static readonly ConcurrentDictionary<string, Task<LeviModDetail>> DetailCache = new();

        private static LeviModsIndexResponse _cachedIndex;

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("BedrockBoot");
            return client;
        }

        public static async Task<LeviModsIndexResponse> GetDataAsync()
        {
            if (_cachedIndex != null) return _cachedIndex;

            await IndexLock.WaitAsync();
            try
            {
                if (_cachedIndex != null) return _cachedIndex;

                var json = await HttpClient.GetStringAsync($"{ApiBase}/index.json");
                _cachedIndex = JsonSerializer.Deserialize<LeviModsIndexResponse>(json, JsonOptions);

                if (_cachedIndex?.Mods == null)
                    throw new Exception("levimc.org 索引解析失败");

                return _cachedIndex;
            }
            finally
            {
                IndexLock.Release();
            }
        }

        public static async Task<LeviModsIndexResponse> RefreshDataAsync()
        {
            _cachedIndex = null;
            DetailCache.Clear();
            return await GetDataAsync();
        }

        /// <summary>获取模组详情（含版本/变体/readme），按 id 记忆化缓存，失败不缓存以便重试。</summary>
        public static async Task<LeviModDetail> GetModDetailAsync(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return null;

            // 接口 id 大小写敏感：依赖键可能来自清单（大小写不一），先对索引做大小写无关归一
            var canonical = await ResolveCanonicalIdAsync(modId);
            var key = canonical ?? modId;

            var task = DetailCache.GetOrAdd(key, FetchDetailAsync);
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                DetailCache.TryRemove(key, out _);
                throw;
            }
        }

        private static async Task<string> ResolveCanonicalIdAsync(string modId)
        {
            try
            {
                var index = await GetDataAsync();
                var mods = index?.Mods;
                if (mods == null) return modId;
                if (mods.Any(m => string.Equals(m.Id, modId, StringComparison.Ordinal))) return modId;

                return mods.FirstOrDefault(m => string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase))?.Id
                       ?? modId;
            }
            catch
            {
                return modId;
            }
        }

        private static async Task<LeviModDetail> FetchDetailAsync(string modId)
        {
            var json = await HttpClient.GetStringAsync($"{ApiBase}/{modId}/index.json");
            return JsonSerializer.Deserialize<LeviModDetail>(json, JsonOptions);
        }

        /// <summary>挑一个客户端侧变体标签：优先字面 "client"，其次任意 side=client 的变体；无客户端变体返回 null。</summary>
        public static string SelectClientVariantLabel(LeviModVersion version)
        {
            var variants = version?.Variants;
            if (variants == null || variants.Count == 0) return null;

            if (variants.ContainsKey("client")) return "client";

            foreach (var kv in variants)
                if (kv.Value?.Any(a => string.Equals(a.Side, "client", StringComparison.OrdinalIgnoreCase)) == true)
                    return kv.Key;

            return null;
        }

        /// <summary>取指定标签下的变体资产，优先 side=client 的那一项。</summary>
        public static LeviVariantAsset GetVariantAsset(LeviModVersion version, string label)
        {
            if (version?.Variants == null || version.Variants.Count == 0) return null;

            List<LeviVariantAsset> pool = null;
            if (!string.IsNullOrEmpty(label) && version.Variants.TryGetValue(label, out var labeled))
                pool = labeled;

            if (pool == null)
            {
                if (version.Variants.TryGetValue("client", out var client)) pool = client;
                pool ??= version.Variants.Values
                    .FirstOrDefault(v => v?.Any(a => string.Equals(a.Side, "client", StringComparison.OrdinalIgnoreCase)) == true)
                    ?? version.Variants.Values.FirstOrDefault();
            }

            return pool?.FirstOrDefault(a => string.Equals(a.Side, "client", StringComparison.OrdinalIgnoreCase))
                   ?? pool?.FirstOrDefault();
        }

        public const string LeviLaminaModId = "github.com/LiteLDev/LeviLamina";

        private static string StripLabel(string depId)
        {
            if (string.IsNullOrEmpty(depId)) return depId;
            var hashIndex = depId.LastIndexOf('#');
            return hashIndex > 0 ? depId.Substring(0, hashIndex) : depId;
        }

        /// <summary>从变体依赖里取 LeviLamina 版本约束（依赖键可携带 #label）。</summary>
        public static string GetLeviLaminaSpec(LeviVariantAsset asset)
        {
            return asset?.Deps?.FirstOrDefault(kv =>
                string.Equals(StripLabel(kv.Key), LeviLaminaModId, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        ///     把 LeviLamina 版本约束解析成具体版本号，并从该版本 server 资产的 bds 依赖推导 Minecraft 版本，
        ///     用于版本列表「Minecraft x.y.z / LeviLamina a.b.c」分组。失败返回 (null, null)。
        /// </summary>
        public static async Task<(string LlVersion, string McVersion)> ResolveTargetVersionAsync(string llSpec)
        {
            string llVersion = null;
            string mcVersion = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(llSpec))
                {
                    var detail = await GetModDetailAsync(LeviLaminaModId);
                    llVersion = LeviVersionSpec.SelectBest(
                        detail?.Versions?.Where(v => !string.IsNullOrEmpty(v.Version)).Select(v => v.Version), llSpec);
                }
            }
            catch
            {
                // 解析失败按未分组展示
            }

            if (string.IsNullOrEmpty(llVersion)) return (null, null);

            try
            {
                var detail = await GetModDetailAsync(LeviLaminaModId);
                var llVer = detail?.Versions?.FirstOrDefault(v =>
                    string.Equals(v.Version, llVersion, StringComparison.Ordinal));
                var serverAsset = llVer?.Variants?.Values
                    .SelectMany(v => v ?? new List<LeviVariantAsset>())
                    .FirstOrDefault(a => string.Equals(a.Side, "server", StringComparison.OrdinalIgnoreCase));

                mcVersion = serverAsset?.Deps?.FirstOrDefault(kv =>
                    string.Equals(StripLabel(kv.Key), "github.com/LiteLDev/bds", StringComparison.OrdinalIgnoreCase)).Value;
            }
            catch
            {
                // LL 详情拿不到时只展示 LeviLamina 版本行
            }

            return (llVersion, mcVersion);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = false
        };
    }

    /// <summary>
    ///     依赖版本区间匹配。清单里出现的写法：
    ///     通配 "26.60.*"、"26.*"、"*"；精确 "1.16.8"（含预发布 "26.60.6-server.17"）；
    ///     区间 "&gt;26.60.8 &lt;26.61.0"（空格/逗号分隔 AND，"||" 分隔 OR）。
    /// </summary>
    public static class LeviVersionSpec
    {
        public static string SelectBest(IEnumerable<string> versions, string spec)
        {
            if (versions == null) return null;

            var parsed = versions
                .Select(v => (raw: v, parts: Parse(v)))
                .Where(x => x.parts != null && Matches(x.raw, spec))
                .ToList();

            if (parsed.Count == 0) return null;

            // 稳定版优先，其次取最高版本
            parsed.Sort((a, b) =>
            {
                var stable = (b.parts.IsPre ? 0 : 1) - (a.parts.IsPre ? 0 : 1);
                if (stable != 0) return stable;
                return Compare(b.parts, a.parts);
            });

            return parsed[0].raw;
        }

        public static bool Matches(string version, string spec)
        {
            if (string.IsNullOrWhiteSpace(spec) || spec.Trim() == "*") return true;
            var parts = Parse(version);
            if (parts == null) return false;

            return spec.Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(alternative => alternative
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .All(constraint => MatchesConstraint(parts, version, constraint)));
        }

        private static bool MatchesConstraint(VersionParts version, string rawVersion, string constraint)
        {
            if (string.IsNullOrWhiteSpace(constraint)) return true;

            string op;
            string body;
            if (constraint.StartsWith(">=")) { op = ">="; body = constraint.Substring(2); }
            else if (constraint.StartsWith("<=")) { op = "<="; body = constraint.Substring(2); }
            else if (constraint.StartsWith(">")) { op = ">"; body = constraint.Substring(1); }
            else if (constraint.StartsWith("<")) { op = "<"; body = constraint.Substring(1); }
            else if (constraint.StartsWith("^")) { op = "^"; body = constraint.Substring(1); }
            else if (constraint.StartsWith("~")) { op = "~"; body = constraint.Substring(1); }
            else if (constraint.StartsWith("=")) { op = "="; body = constraint.Substring(1); }
            else { op = "="; body = constraint; }

            if (body == "*" || body == "x" || body == "X") return true;

            if (op == "=" && body.Contains('*'))
            {
                // 通配只比对核心前缀
                var prefix = Parse(body.TrimEnd('*').TrimEnd('.'));
                if (prefix == null) return false;
                for (var i = 0; i < prefix.Core.Length; i++)
                {
                    var segment = i < version.Core.Length ? version.Core[i] : 0;
                    if (segment != prefix.Core[i]) return false;
                }

                return true;
            }

            var target = Parse(body);
            if (target == null) return false;

            switch (op)
            {
                case ">": return Compare(version, target) > 0;
                case ">=": return Compare(version, target) >= 0;
                case "<": return Compare(version, target) < 0;
                case "<=": return Compare(version, target) <= 0;
                case "^":
                case "~":
                    if (Compare(version, target) < 0) return false;
                    // ^1.2.3 → <2.0.0；^0.2.3 → <0.3.0；^0.0.3 → <0.0.4
                    // ~1.2.3 → <1.3.0；~1 → <2.0.0
                    var upper = op == "^"
                        ? UpperBoundForCaret(target)
                        : UpperBoundForTilde(target);
                    return Compare(version, upper) < 0;
                case "=":
                default:
                    return Compare(version, target) == 0 &&
                           string.Equals(version.Pre, target.Pre, StringComparison.Ordinal);
            }
        }

        private static VersionParts UpperBoundForCaret(VersionParts v)
        {
            long[] core;
            if (v.Core.Length > 0 && v.Core[0] > 0)
                core = new[] { v.Core[0] + 1, 0, 0 };
            else if (v.Core.Length > 1 && v.Core[1] > 0)
                core = new[] { 0, v.Core[1] + 1, 0 };
            else
                core = new[] { 0, 0, (v.Core.Length > 2 ? v.Core[2] : 0) + 1 };
            return new VersionParts { Core = core, Pre = null, IsPre = false };
        }

        private static VersionParts UpperBoundForTilde(VersionParts v)
        {
            long[] core;
            if (v.Core.Length > 1)
                core = new[] { v.Core[0], v.Core[1] + 1, 0 };
            else
                core = new[] { (v.Core.Length > 0 ? v.Core[0] : 0) + 1, 0, 0 };
            return new VersionParts { Core = core, Pre = null, IsPre = false };
        }

        private static int Compare(VersionParts a, VersionParts b)
        {
            var length = Math.Max(a.Core.Length, b.Core.Length);
            for (var i = 0; i < length; i++)
            {
                var left = i < a.Core.Length ? a.Core[i] : 0;
                var right = i < b.Core.Length ? b.Core[i] : 0;
                if (left != right) return left.CompareTo(right);
            }

            if (a.IsPre != b.IsPre) return a.IsPre ? -1 : 1;
            if (a.IsPre && b.IsPre)
                return string.Compare(a.Pre, b.Pre, StringComparison.Ordinal);

            return 0;
        }

        private static VersionParts Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return null;

            var text = version.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase) && text.Length > 1)
                text = text.Substring(1);

            var plusIndex = text.IndexOf('+');
            if (plusIndex >= 0) text = text.Substring(0, plusIndex);

            string pre = null;
            var dashIndex = text.IndexOf('-');
            if (dashIndex >= 0)
            {
                pre = text.Substring(dashIndex + 1);
                text = text.Substring(0, dashIndex);
            }

            var segments = text.Split('.');
            if (segments.Length == 0) return null;

            var core = new long[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                if (!long.TryParse(segments[i], out var value)) return null;
                core[i] = value;
            }

            return new VersionParts { Core = core, Pre = pre, IsPre = pre != null };
        }

        private class VersionParts
        {
            public long[] Core { get; set; }
            public string Pre { get; set; }
            public bool IsPre { get; set; }
        }
    }
}
