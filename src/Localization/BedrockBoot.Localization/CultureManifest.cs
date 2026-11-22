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

using System.Text.Json;

namespace BedrockBoot.Localization;

/// <summary>
///     语言清单：哪几门语言存在、各自显示成什么、回退到谁，以及基准语与外语兜底语。
/// </summary>
/// <remarks>
///     清单是数据（<c>Resources/I18n/cultures.json</c>），不是代码——加一门语言只需
///     放一个 <c>{code}.axaml</c> 并在清单里加一行，不必改 C# 再重新编译宿主。
///     <para>
///         <c>parent</c> 同时是"回退目标"的唯一定义处：<see cref="BuildFallbacks" /> 由它生成
///         <see cref="LocalizationOptions.CultureFallbacks" />，避免语言列表与回退关系两处维护。
///     </para>
///     <para>
///         <see cref="Builtin" /> 是<b>全函数</b>：资源缺失、JSON 损坏、字段非法都只会退回
///         <see cref="Default" /> 并记一条警告，绝不抛异常——它会在
///         <see cref="LocalizationContext" /> 的静态初始化路径上被求值，抛出去就是启动即崩。
///     </para>
/// </remarks>
public sealed class CultureManifest
{
    /// <summary>清单在语言目录内的文件名。</summary>
    public const string FileName = "cultures.json";

    private static readonly Lazy<CultureManifest> LazyBuiltin =
        new(LoadBuiltin, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Dictionary<string, string> _parents;

    private CultureManifest(
        string baseline,
        string fallback,
        IReadOnlyList<CultureEntry> cultures,
        Dictionary<string, string> parents)
    {
        Baseline = baseline;
        Fallback = fallback;
        Cultures = cultures;
        _parents = parents;
    }

    /// <summary>内置清单（随本程序集打包）。解析失败时返回 <see cref="Default" />。</summary>
    public static CultureManifest Builtin => LazyBuiltin.Value;

    /// <summary>清单不可用时的兜底清单，与重构前的硬编码内容一致。</summary>
    public static CultureManifest Default { get; } = CreateDefault();

    /// <summary>键集合的权威来源，同时是最终兜底语言。</summary>
    public string Baseline { get; }

    /// <summary>外语的统一兜底语言。</summary>
    public string Fallback { get; }

    /// <summary>按清单顺序排列的语言。</summary>
    public IReadOnlyList<CultureEntry> Cultures { get; }

    /// <summary>从清单里的 <c>parent</c> 生成回退关系表。</summary>
    public IReadOnlyDictionary<string, string> BuildFallbacks() =>
        new Dictionary<string, string>(_parents, StringComparer.OrdinalIgnoreCase);

    /// <summary>把清单里的语言登记到登记表；已登记的保持原样（不覆盖外部注册的显示名）。</summary>
    public void RegisterInto(CultureRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var entry in Cultures)
        {
            if (!registry.Contains(entry.Culture))
                registry.Register(entry.Culture, entry.DisplayName);
        }
    }

    /// <summary>解析一份清单。</summary>
    /// <remarks>
    ///     用 <see cref="JsonDocument" /> 而不是反序列化到 DTO：不依赖反射，
    ///     <c>PublishTrimmed</c> / AOT 下行为一致，也不需要维护一份与 JSON 同构的类型。
    /// </remarks>
    /// <exception cref="JsonException">内容不是合法 JSON 或缺少 <c>cultures</c> 数组。</exception>
    public static CultureManifest Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var baseline = ReadString(root, "baseline") ?? Default.Baseline;
        var fallback = ReadString(root, "fallback") ?? Default.Fallback;

        if (!root.TryGetProperty("cultures", out var array) || array.ValueKind != JsonValueKind.Array)
            throw new JsonException("清单缺少 cultures 数组");

        var cultures = new List<CultureEntry>(array.GetArrayLength());
        var parents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var code = ReadString(item, "code");
            if (string.IsNullOrWhiteSpace(code))
            {
                LocalizationLog.Warn("清单里有一条语言缺少 code，已跳过");
                continue;
            }

            var displayName = ReadString(item, "displayName");
            cultures.Add(new CultureEntry(code, string.IsNullOrWhiteSpace(displayName) ? code : displayName));

            var parent = ReadString(item, "parent");
            if (!string.IsNullOrWhiteSpace(parent) && !string.Equals(parent, code, StringComparison.OrdinalIgnoreCase))
                parents[code] = parent;
        }

        if (cultures.Count == 0)
            throw new JsonException("清单里没有任何语言");

        return new CultureManifest(baseline, fallback, cultures, parents);
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static CultureManifest LoadBuiltin()
    {
        try
        {
            var assembly = typeof(CultureManifest).Assembly;
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + FileName, StringComparison.OrdinalIgnoreCase));

            if (name is null)
            {
                LocalizationLog.Warn($"找不到内置语言清单 {FileName}，改用内置默认值");
                return Default;
            }

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                LocalizationLog.Warn($"无法读取内置语言清单 {FileName}，改用内置默认值");
                return Default;
            }

            return Parse(stream);
        }
        catch (Exception ex)
        {
            LocalizationLog.Warn($"内置语言清单解析失败（{ex.GetType().Name}：{ex.Message}），改用内置默认值");
            return Default;
        }
    }

    private static CultureManifest CreateDefault() =>
        new(
            "zh_CN",
            "en_US",
            new[]
            {
                new CultureEntry("zh_CN", "中文（简体）"),
                new CultureEntry("zh_TW", "中文（繁體）"),
                new CultureEntry("yue_HK", "中文（粵語）"),
                new CultureEntry("en_US", "English (US)"),
                new CultureEntry("ja_JP", "日本語"),
                new CultureEntry("ru_RU", "Русский"),
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh_TW"] = "zh_CN",
                ["yue_HK"] = "zh_CN",
            });
}
