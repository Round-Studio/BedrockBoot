using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BedrockBoot.Views.DialogContent;
using Octokit;

namespace BedrockBoot.Models.Helper;

/// <summary>
/// 联机依赖组件（EasyTier / GravityCone CLI）的版本记录与更新检查。
///
/// <para>
/// 每个组件目录下存放一个 version.json 记录已安装的版本号（GitHub Release Tag）。
/// 首次下载与后续更新均由 <see cref="Views.DialogContent.DialogDownloadMultiPlayerDependenceContent"/>
/// 在解压成功后调用 <see cref="WriteLocalVersion"/> 写入。
/// </para>
/// </summary>
public static class MultiplayerDependencyHelper
{
    public const string VersionFileName = "version.json";

    /// <summary>EasyTier 的 GitHub 仓库</summary>
    public const string EasyTierOwner = "EasyTier";
    public const string EasyTierRepo = "EasyTier";

    /// <summary>GravityCone 的 GitHub 仓库</summary>
    public const string GravityConeOwner = "Tianpao";
    public const string GravityConeRepo = "GravityCone";

    /// <summary>组件版本记录文件的内容</summary>
    public class DependencyVersionEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("installTime")] public DateTime InstallTime { get; set; }
    }

    /// <summary>单个组件的更新检查结果</summary>
    public class DependencyStatus
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>本地已安装版本；未安装或缺少版本记录时为 null</summary>
        public string? LocalVersion { get; set; }

        /// <summary>远端最新版本；获取失败时为 null</summary>
        public string? LatestVersion { get; set; }

        /// <summary>组件主体文件是否存在</summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// 是否有可用更新。
        /// 已安装但缺少版本记录（旧版启动器安装的）也视为可更新，以便补齐记录。
        /// </summary>
        public bool HasUpdate =>
            IsInstalled &&
            LatestVersion != null &&
            !string.Equals(NormalizeVersion(LocalVersion), NormalizeVersion(LatestVersion),
                StringComparison.OrdinalIgnoreCase);
    }

    public static string EasyTierVersionFile =>
        Path.Combine(DialogDownloadMultiPlayerDependenceContent.EasyTierPath, VersionFileName);

    public static string GravityConeVersionFile =>
        Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath, VersionFileName);

    /// <summary>去掉 tag 常见的 v 前缀再比较，避免 v2.4.5 与 2.4.5 被误判为不同版本</summary>
    private static string? NormalizeVersion(string? version)
        => string.IsNullOrWhiteSpace(version) ? null : version.Trim().TrimStart('v', 'V');

    /// <summary>读取组件目录下的本地版本记录；文件缺失或损坏时返回 null</summary>
    public static string? GetLocalVersion(string versionFilePath)
    {
        try
        {
            if (!File.Exists(versionFilePath)) return null;
            var entry = JsonSerializer.Deserialize<DependencyVersionEntry>(File.ReadAllText(versionFilePath));
            return string.IsNullOrWhiteSpace(entry?.Version) ? null : entry.Version;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取依赖版本记录失败 ({versionFilePath}): {ex.Message}");
            return null;
        }
    }

    /// <summary>将安装的版本号写入组件目录下的版本记录文件</summary>
    public static void WriteLocalVersion(string versionFilePath, string name, string version)
    {
        try
        {
            var dir = Path.GetDirectoryName(versionFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var entry = new DependencyVersionEntry
            {
                Name = name,
                Version = version,
                InstallTime = DateTime.Now
            };
            File.WriteAllText(versionFilePath,
                JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"写入依赖版本记录失败 ({versionFilePath}): {ex.Message}");
        }
    }

    /// <summary>组件是否已安装（主体文件存在）</summary>
    public static bool IsEasyTierInstalled() =>
        File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.EasyTierPath, "easytier-cli.exe"));

    public static bool IsGravityConeInstalled() =>
        File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath,
            "gravitycone-cli-windows-amd64.exe"));

    /// <summary>
    /// 查询两个组件的本地版本与 GitHub 最新版本。
    /// 网络失败时抛出异常，由调用方负责提示。
    /// </summary>
    public static async Task<(DependencyStatus EasyTier, DependencyStatus GravityCone)> CheckUpdatesAsync()
    {
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

        var easyTierTask = github.Repository.Release.GetLatest(EasyTierOwner, EasyTierRepo);
        var gravityConeTask = github.Repository.Release.GetLatest(GravityConeOwner, GravityConeRepo);

        await Task.WhenAll(easyTierTask, gravityConeTask);

        var easyTier = new DependencyStatus
        {
            Name = "EasyTier",
            LocalVersion = GetLocalVersion(EasyTierVersionFile),
            LatestVersion = easyTierTask.Result.TagName,
            IsInstalled = IsEasyTierInstalled()
        };

        var gravityCone = new DependencyStatus
        {
            Name = "GravityCone",
            LocalVersion = GetLocalVersion(GravityConeVersionFile),
            LatestVersion = gravityConeTask.Result.TagName,
            IsInstalled = IsGravityConeInstalled()
        };

        return (easyTier, gravityCone);
    }
}
