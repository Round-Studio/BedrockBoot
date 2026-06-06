using System.Text.RegularExpressions;

namespace BedrockBoot.Models.Helper.Uwp;

/// <summary>
/// 来自 BMCBL 的技术支持
/// https://github.com/Chlna6666/Better-Minecraft-Bedrock-Launcher/blob/master/src-tauri/src/utils/mc_dependency.rs
/// </summary>
public class UwpFileUrl
{
    public static async Task<string> GetUwpPackageDownloadUrl(string packageName, string? minVersion = null)
    {
        string pfn = $"{packageName}_8wekyb3d8bbwe";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Origin", "https://store.rg-adguard.net");
        client.DefaultRequestHeaders.Add("Referer", "https://store.rg-adguard.net/");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("type", "PackageFamilyName"),
            new KeyValuePair<string, string>("url", pfn),
            new KeyValuePair<string, string>("ring", "RP"),
            new KeyValuePair<string, string>("lang", "en-US")
        });

        var response = await client.PostAsync("https://store.rg-adguard.net/api/GetFiles", content);
        var html = await response.Content.ReadAsStringAsync();

        // 匹配下载链接（x64/neutral + appx/appxbundle/msixbundle/msix）
        var regex = new Regex(@"<a\s+href=""([^""]+)""[^>]*>([^<]+\.(?:appx|appxbundle|msixbundle|msix))</a>",
            RegexOptions.IgnoreCase);
        var matches = regex.Matches(html);

        string bestUrl = null;
        string bestVersion = null;

        foreach (Match match in matches)
        {
            string url = match.Groups[1].Value;
            string fileName = match.Groups[2].Value.ToLower();

            // 只选择 x64 或 neutral 架构
            if (!fileName.Contains("x64") && !fileName.Contains("neutral"))
                continue;

            // 提取版本号
            var versionMatch = Regex.Match(fileName, @"(\d+\.\d+\.\d+\.\d+)");
            string version = versionMatch.Success ? versionMatch.Groups[1].Value : null;

            // 选择最高版本
            if (bestUrl == null || CompareVersions(version, bestVersion) > 0)
            {
                bestUrl = url;
                bestVersion = version;
            }
        }

        // 检查版本要求
        if (!string.IsNullOrEmpty(minVersion) && CompareVersions(bestVersion, minVersion) < 0)
        {
            Console.WriteLine($@"警告: 最高版本 {bestVersion} 低于最低要求 {minVersion}");
            return null;
        }

        return bestUrl;
    }

// 版本比较辅助函数
    static int CompareVersions(string v1, string v2)
    {
        if (v1 == v2) return 0;
        if (v1 == null) return -1;
        if (v2 == null) return 1;

        var p1 = v1.Split('.').Select(int.Parse).ToArray();
        var p2 = v2.Split('.').Select(int.Parse).ToArray();

        for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
        {
            int n1 = i < p1.Length ? p1[i] : 0;
            int n2 = i < p2.Length ? p2[i] : 0;
            if (n1 != n2) return n1.CompareTo(n2);
        }

        return 0;
    }
}