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
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.Loaders.LeviLamina;

public class LeviLaminaVersionDb
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }

    [JsonPropertyName("versions")] public Dictionary<string, List<string>> Versions { get; set; }

    private static LeviLaminaVersionDb _cache;
    private static DateTime _cacheTime;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
    private static readonly object _lock = new object();

    public static async Task<LeviLaminaVersionDb> FetchAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && IsCacheValid())
        {
            return _cache;
        }

        try
        {
            // 与主程序 GitHub 下载一致的加速源竞速方式，取代 jsdelivr 直链
            string tempFile = Path.Combine(Path.GetTempPath(), $"levilamina-versiondb-{Guid.NewGuid():N}.json");
            var downloader = new GithubFilesDownloader();
            await downloader.DownloadAsync(
                "https://github.com/LiteLDev/levilamina-client-version-db/raw/main/v2/version-db.json",
                tempFile, null, CancellationToken.None);

            string json = await File.ReadAllTextAsync(tempFile);
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<LeviLaminaVersionDb>(json, options);

            lock (_lock)
            {
                _cache = result;
                _cacheTime = DateTime.UtcNow;
            }

            return result;
        }
        catch
        {
            return _cache;
        }
    }

    public static LeviLaminaVersionDb Fetch(bool forceRefresh = false)
    {
        return FetchAsync(forceRefresh).GetAwaiter().GetResult();
    }

    private static bool IsCacheValid()
    {
        lock (_lock)
        {
            return _cache != null && DateTime.UtcNow - _cacheTime < _cacheDuration;
        }
    }

    public static void ClearCache()
    {
        lock (_lock)
        {
            _cache = null;
            _cacheTime = DateTime.MinValue;
        }
    }

    public List<string> GetLeviLaminaVersions(string bedrockVersion)
    {
        if (Versions == null || string.IsNullOrEmpty(bedrockVersion))
            return new List<string>();

        return Versions.TryGetValue(bedrockVersion, out var versions) ? versions : new List<string>();
    }

    public string GetLatestLeviLaminaVersion(string bedrockVersion)
    {
        var versions = GetLeviLaminaVersions(bedrockVersion);
        return versions.Count > 0 ? versions[versions.Count - 1] : null;
    }

    public bool IsVersionCompatible(string bedrockVersion, string leviLaminaVersion)
    {
        var versions = GetLeviLaminaVersions(bedrockVersion);
        return versions.Contains(leviLaminaVersion);
    }
}