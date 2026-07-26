using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Core.Models.Helper;

/// <summary>
/// 游戏安装包（.insPack）的全局缓存索引。
///
/// <para>
/// 索引文件与启动器配置文件同目录（BedrockBoot.Config/GamePackageCache.json），
/// 记录每个已缓存版本的：版本号、构建类型、文件路径、MD5、缓存时间。
/// </para>
///
/// <para>
/// 游戏包缓存本体按安装目录存放在 {目录}/version_save/{版本}.insPack。
/// 有了全局索引后，EasyDownload 在任意目录安装时都能复用其他目录中已缓存的包，
/// 不再重复下载同一版本。
/// </para>
/// </summary>
public static class GamePackageCacheIndex
{
    /// <summary>索引文件路径：与 Config.json 同目录</summary>
    public static readonly string IndexFilePath =
        Path.Combine(PathsList.RootConfigPath, "BedrockBoot.Config", "GamePackageCache.json");

    private static readonly object Gate = new();

    public class CacheEntry
    {
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("buildType")] public string BuildType { get; set; } = string.Empty;
        [JsonPropertyName("filePath")] public string FilePath { get; set; } = string.Empty;
        [JsonPropertyName("md5")] public string Md5 { get; set; } = string.Empty;
        [JsonPropertyName("cachedTime")] public DateTime CachedTime { get; set; }
    }

    private static List<CacheEntry> Load()
    {
        try
        {
            if (!File.Exists(IndexFilePath)) return new List<CacheEntry>();
            return JsonSerializer.Deserialize<List<CacheEntry>>(File.ReadAllText(IndexFilePath))
                   ?? new List<CacheEntry>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取游戏包缓存索引失败: {ex.Message}");
            return new List<CacheEntry>();
        }
    }

    private static void Save(List<CacheEntry> entries)
    {
        try
        {
            var dir = Path.GetDirectoryName(IndexFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(IndexFilePath,
                JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"写入游戏包缓存索引失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 查找指定版本的缓存条目。
    /// 仅返回文件仍然存在的条目；文件已被删除的失效条目会被顺手清理。
    /// </summary>
    public static CacheEntry? Find(string version, string buildType)
    {
        lock (Gate)
        {
            var entries = Load();
            var removed = entries.RemoveAll(x => !File.Exists(x.FilePath));

            var entry = entries.Find(x =>
                string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.BuildType, buildType, StringComparison.OrdinalIgnoreCase));

            if (removed > 0) Save(entries);
            return entry;
        }
    }

    /// <summary>
    /// 登记（或更新）一个缓存条目。以 版本号 + 构建类型 + 文件路径 作为去重键。
    /// </summary>
    public static void Register(string version, string buildType, string filePath, string md5)
    {
        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        lock (Gate)
        {
            var entries = Load();

            // 同版本同路径的旧条目直接替换；同版本不同路径的条目保留（多个目录各有一份缓存是合法的），
            // 但 Find 只需要任意一份，故这里将最新登记的排到最前
            entries.RemoveAll(x =>
                string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.BuildType, buildType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            entries.Insert(0, new CacheEntry
            {
                Version = version,
                BuildType = buildType,
                FilePath = filePath,
                Md5 = md5,
                CachedTime = DateTime.Now
            });

            Save(entries);
        }
    }
}
