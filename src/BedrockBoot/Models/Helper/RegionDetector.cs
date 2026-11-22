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
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Core.Global;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     地区判定器：综合公网 IP 归属地、系统时区、系统地区、系统语言与启动器语言
///     五路信号加权打分，判断当前设备是否位于中国境内（含中国香港、澳门、台湾）。
///     结果进程内缓存，只判定一次。
/// </summary>
public static class RegionDetector
{
    /// <summary>中国境内地区码（含港澳台）</summary>
    private static readonly HashSet<string> ChinaRegions = new(StringComparer.OrdinalIgnoreCase)
        { "CN", "HK", "MO", "TW" };

    /// <summary>启动器语言键（I18nManager 注册键）中属于境内的取值</summary>
    private static readonly HashSet<string> ChinaLauncherLanguages = new(StringComparer.OrdinalIgnoreCase)
        { "zh_CN", "zh_TW", "yue_HK" };

    /// <summary>Windows / IANA 时区 ID 到境内地区码的映射</summary>
    private static readonly Dictionary<string, string> ChinaTimezones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["China Standard Time"] = "CN",
        ["Taipei Standard Time"] = "TW",
        ["Asia/Shanghai"] = "CN",
        ["Asia/Urumqi"] = "CN",
        ["Asia/Hong_Kong"] = "HK",
        ["Asia/Macau"] = "MO",
        ["Asia/Taipei"] = "TW"
    };

    // 各信号权重：IP 归属地占绝对主导，其余信号在离线或 IP 查询失败时兜底
    private const double ScoreIpInChina = 100;
    private const double ScoreIpForeign = -100;
    private const double ScoreTimezoneInChina = 25;
    private const double ScoreTimezoneForeign = -15;
    private const double ScoreSystemRegionInChina = 20;
    private const double ScoreSystemRegionForeign = -15;
    private const double ScoreSystemLanguageInChina = 10;
    private const double ScoreSystemLanguageForeign = -10;
    private const double ScoreLauncherLanguageInChina = 10;
    private const double ScoreLauncherLanguageForeign = -10;

    /// <summary>综合得分达到该阈值判定为境内；全零（完全未知）时保守判定为境外</summary>
    private const double InChinaThreshold = 40;

    private const int IpQueryTimeoutSeconds = 6;

    /// <summary>单条信号记录，用于诊断各信号贡献</summary>
    public sealed class RegionSignal
    {
        public string Name = string.Empty;
        public string Value = string.Empty;
        public double Score;

        public override string ToString() => $"{Name}={Value} ({Score:+0;-0})";
    }

    private static List<RegionSignal> _lastSignals = new();

    /// <summary>最近一次判定的信号明细</summary>
    public static IReadOnlyList<RegionSignal> LastSignals => _lastSignals;

    /// <summary>最近一次判定的综合得分</summary>
    public static double LastScore { get; private set; }

    /// <summary>是否已完成一次判定</summary>
    public static bool IsDetected => _detectionTask is { IsCompleted: true };

    /// <summary>
    ///     判定结果同步读取。判定完成前默认按境内处理（境外场景由检测完成后回调隐藏）。
    /// </summary>
    public static bool IsInChina { get; private set; } = true;

    private static Task<bool>? _detectionTask;

    /// <summary>
    ///     判定当前设备是否位于中国境内（含港澳台）。结果进程内缓存，只判定一次。
    /// </summary>
    public static Task<bool> IsInChinaAsync()
    {
        if (_detectionTask != null) return _detectionTask;
        _detectionTask = DetectAsync();
        return _detectionTask;
    }

    private static async Task<bool> DetectAsync()
    {
        // 调试：设置环境变量 BEDROCKBOOT_FORCE_REGION=foreign / china 可强制指定判定结果，
        // 便于在境内设备上验证境外隐藏路径
        var forced = Environment.GetEnvironmentVariable("BEDROCKBOOT_FORCE_REGION");
        if (string.Equals(forced, "foreign", StringComparison.OrdinalIgnoreCase))
        {
            IsInChina = false;
            LastScore = 0;
            _lastSignals = new List<RegionSignal>();
            Console.WriteLine("RegionDetector 环境变量强制判定为境外");
            return false;
        }

        if (string.Equals(forced, "china", StringComparison.OrdinalIgnoreCase))
        {
            IsInChina = true;
            LastScore = 0;
            _lastSignals = new List<RegionSignal>();
            Console.WriteLine("RegionDetector 环境变量强制判定为境内");
            return true;
        }

        var signals = new List<RegionSignal>();
        var score = 0d;

        // 1. 公网 IP 归属地（最高权重）
        var ipRegion = await QueryIpRegionAsync();
        if (ipRegion != null)
        {
            var s = ChinaRegions.Contains(ipRegion) ? ScoreIpInChina : ScoreIpForeign;
            score += s;
            signals.Add(new RegionSignal { Name = "IP", Value = ipRegion, Score = s });
        }

        // 2. 系统时区
        var tzRegion = GetTimezoneRegion();
        if (tzRegion != null)
        {
            var s = ChinaRegions.Contains(tzRegion) ? ScoreTimezoneInChina : ScoreTimezoneForeign;
            score += s;
            signals.Add(new RegionSignal { Name = "Timezone", Value = tzRegion, Score = s });
        }

        // 3. 系统地区
        var sysRegion = GetSystemRegion();
        if (sysRegion != null)
        {
            var s = ChinaRegions.Contains(sysRegion) ? ScoreSystemRegionInChina : ScoreSystemRegionForeign;
            score += s;
            signals.Add(new RegionSignal { Name = "SystemRegion", Value = sysRegion, Score = s });
        }

        // 4. 系统语言
        var sysLang = GetSystemLanguageRegion();
        if (sysLang != null)
        {
            var s = ChinaRegions.Contains(sysLang) ? ScoreSystemLanguageInChina : ScoreSystemLanguageForeign;
            score += s;
            signals.Add(new RegionSignal { Name = "SystemLanguage", Value = sysLang, Score = s });
        }

        // 5. 启动器语言
        var launcherLang = GetLauncherLanguage();
        if (launcherLang != null)
        {
            var s = ChinaLauncherLanguages.Contains(launcherLang)
                ? ScoreLauncherLanguageInChina
                : ScoreLauncherLanguageForeign;
            score += s;
            signals.Add(new RegionSignal { Name = "LauncherLanguage", Value = launcherLang, Score = s });
        }

        var inChina = score >= InChinaThreshold;
        LastScore = score;
        _lastSignals = signals;
        IsInChina = inChina;
        Console.WriteLine($"RegionDetector 综合得分 {score:+0;-0} → {(inChina ? "境内" : "境外")} " +
                          $"[{string.Join(", ", signals)}]");
        return inChina;
    }

    // --- 各信号采集 ---

    /// <summary>并发查询多个免费 IP 归属地服务，取最先成功的结果；全部失败返回 null</summary>
    private static async Task<string?> QueryIpRegionAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(IpQueryTimeoutSeconds));
        var token = timeout.Token;

        var tasks = new[]
        {
            FetchIpCountryCodeAsync("http://ip-api.com/json/?fields=status,countryCode", token,
                root => root.TryGetProperty("status", out var st) && st.GetString() == "success"
                    ? ReadString(root, "countryCode")
                    : null),
            FetchIpCountryCodeAsync("https://api.ip.sb/geoip", token,
                root => ReadString(root, "country_code", "countryCode")),
            FetchIpCountryCodeAsync("https://ipwho.is/", token,
                root => root.TryGetProperty("success", out var ok) && ok.ValueKind != JsonValueKind.True
                    ? null
                    : ReadString(root, "country_code", "countryCode"))
        };

        try
        {
            var results = await Task.WhenAll(tasks);
            return results.FirstOrDefault(r => r != null);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> FetchIpCountryCodeAsync(string url, CancellationToken token,
        Func<JsonElement, string?> parse)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(IpQueryTimeoutSeconds);
            await using var stream = await client.GetStreamAsync(url, token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            return parse(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement root, params string[] keys)
    {
        foreach (var key in keys)
            if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }

        return null;
    }

    /// <summary>系统时区映射到地区码；命中境内映射返回地区码，可识别的非境内时区返回原 ID 记负分，无法识别返回 null</summary>
    private static string? GetTimezoneRegion()
    {
        try
        {
            var id = TimeZoneInfo.Local.Id;
            if (string.IsNullOrEmpty(id)) return null;
            return ChinaTimezones.TryGetValue(id, out var region) ? region : id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     系统地区：跨平台方案，从当前区域文化的地区后缀推断（如 zh-CN → CN），
    ///     不调用系统原生 API
    /// </summary>
    private static string? GetSystemRegion()
    {
        try
        {
            var name = CultureInfo.CurrentCulture.Name;
            var separator = name.IndexOfAny(['-', '_']);
            return separator > 0 ? name[(separator + 1)..] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>系统 UI 语言的地区后缀；中性语言 zh 视作 CN，其余中性语言返回原语言名记负分</summary>
    private static string? GetSystemLanguageRegion()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrEmpty(name)) return null;

            var separator = name.IndexOfAny(['-', '_']);
            if (separator > 0) return name[(separator + 1)..];

            // 中性语言（如 "zh"、"en"）
            return name.Equals("zh", StringComparison.OrdinalIgnoreCase) ? "CN" : name;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>启动器语言（I18nManager 语言键，如 zh_CN）；未设置返回 null 不计分</summary>
    private static string? GetLauncherLanguage()
    {
        try
        {
            return GlobalModel.Config?.Data?.Language;
        }
        catch
        {
            return null;
        }
    }
}
