using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core.VersionJsons;

namespace BedrockBoot.Models.Helper;

public class VersionHelper
{
    public static List<BuildInfo> Versions { get; private set; } = null;
    // Event raised when a refreshed version list (from network) replaces the current list
    public static event Action<List<BuildInfo>>? VersionsRefreshed;

    private static readonly object _refreshLock = new();
    private static readonly string CacheFilePath = Path.Combine(PathsList.TempPath, "version_cache.json");

    public static List<BuildInfo> GetVersions()
    {
        if (Versions != null) return Versions;

        // Try to load from disk cache first
        try
        {
            var cached = LoadCache();
            if (cached != null)
            {
                Versions = cached;
            }
        }
        catch
        {
            // ignore cache load errors
        }

        // Always trigger a background refresh from network. If we had no cache, perform a blocking fetch so caller gets data.
        var didHaveCache = Versions != null;

        if (didHaveCache)
        {
            _ = Task.Run(async () => await RefreshFromNetworkAsync());
            return Versions!;
        }

        try
        {
            var fetched = RefreshFromNetworkAsync().GetAwaiter().GetResult();
            Versions = fetched;
            return Versions;
        }
        catch
        {
            return Versions; // may be null
        }
    }

    private static List<BuildInfo>? LoadCache()
    {
        if (!File.Exists(CacheFilePath)) return null;

        var json = File.ReadAllText(CacheFilePath);
        if (string.IsNullOrWhiteSpace(json)) return null;

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var list = JsonSerializer.Deserialize<List<BuildInfo>>(json, opts);
            return list;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveCache(List<BuildInfo> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(list, opts);
            File.WriteAllText(CacheFilePath, json);
        }
        catch
        {
            // ignore cache save errors
        }
    }

    private static async Task<List<BuildInfo>> RefreshFromNetworkAsync()
    {
        lock (_refreshLock)
        {
            // ensure only one refresh at a time
        }

        try
        {
            var url = BedrockBoot.Core.Global.GlobalModel.Config == null
                ? SourceList.VersionDataSources.ToList()[0].Value
                : SourceList.VersionDataSources.ToList()[BedrockBoot.Core.Global.GlobalModel.Config.Data.VersionSourceIndex].Value;

            var db = await VersionsHelper.GetBuildDatabaseAsync(url).ConfigureAwait(false);
            var lst = await db!.Builds.ToListAsync().ConfigureAwait(false);

            var versionCache = new List<(BuildInfo item, Version? version)>();

            foreach (var item in lst)
            {
                if (string.IsNullOrEmpty(item.Value.ID)) continue;
                if (item.Value.Variations.Count <= 0) continue;

                var isCon = false;
                foreach (var v in item.Value.Variations)
                    if (v.MetaData.Count <= 0)
                        isCon = true;

                if (isCon) continue;

                Version? version = null;
                try { version = new Version(item.Value.ID); } catch { }

                versionCache.Add((item.Value, version));
            }

            versionCache.Sort((x, y) =>
            {
                if (x.version != null && y.version != null) return y.version.CompareTo(x.version);
                if (x.version != null) return -1;
                if (y.version != null) return 1;
                return string.Compare(y.item.ID, x.item.ID, StringComparison.Ordinal);
            });

            var sortedList = versionCache.Select(x => x.item).ToList();

            // If different from current cache, replace and notify
            var shouldUpdate = !AreListsEqual(sortedList, Versions);
            if (shouldUpdate)
            {
                Versions = sortedList;
                SaveCache(sortedList);
                VersionsRefreshed?.Invoke(sortedList);
            }

            return Versions ?? sortedList;
        }
        catch
        {
            return Versions ?? new List<BuildInfo>();
        }
    }

    private static bool AreListsEqual(List<BuildInfo>? a, List<BuildInfo>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i].ID, b[i].ID, StringComparison.Ordinal)) return false;
        }

        return true;
    }
}