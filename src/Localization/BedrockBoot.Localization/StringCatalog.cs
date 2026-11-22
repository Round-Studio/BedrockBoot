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

namespace BedrockBoot.Localization;

/// <summary>
///     多来源的字符串目录：负责合并各程序集的键值，并按回退链
///     生成一份“键集合完整、每个键都有值”的对照表。
/// </summary>
/// <remarks>
///     这里的核心不变量是：<b>生成的表在键集合上与语言无关</b>。
///     因此切换语言只需替换值，不需要增删键，UI 层不会出现空缺窗口。
/// </remarks>
public sealed class StringCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(0);

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _sync = new();
    private readonly List<IStringSource> _sources = new();

    /// <summary>已注册的来源，按注册顺序（后者覆盖前者）。</summary>
    public IReadOnlyList<IStringSource> Sources
    {
        get
        {
            lock (_sync)
            {
                return _sources.ToArray();
            }
        }
    }

    /// <summary>注册一个来源。可随时追加（例如插件加载后）。</summary>
    public void Add(IStringSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_sync)
        {
            _sources.Add(source);
            _cache.Clear();
        }
    }

    /// <summary>判断某个来源是否已注册（按 <see cref="IStringSource.Id" /> 比较）。</summary>
    public bool Contains(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        lock (_sync)
        {
            return _sources.Any(source => string.Equals(source.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>清空缓存，下次取表时重新解析所有来源。</summary>
    public void Invalidate()
    {
        lock (_sync)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    ///     取某门语言合并后的原始键值（不套回退）。没有该语言时返回空表。
    /// </summary>
    public IReadOnlyDictionary<string, string> Get(string culture)
    {
        lock (_sync)
        {
            if (_cache.TryGetValue(culture, out var cached))
                return cached;
        }

        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in Sources)
        {
            IReadOnlyDictionary<string, string>? loaded;
            try
            {
                loaded = source.Load(culture);
            }
            catch (Exception ex)
            {
                LocalizationLog.Warn($"来源 {source.Id} 加载 {culture} 失败：{ex.Message}");
                continue;
            }

            if (loaded is null)
                continue;

            foreach (var pair in loaded)
                merged[pair.Key] = pair.Value;
        }

        lock (_sync)
        {
            _cache[culture] = merged;
        }

        return merged;
    }

    /// <summary>
    ///     生成某门语言的完整对照表：键集合为回退链上所有键的并集，
    ///     每个键都按链求值，最差情况取键名本身。
    /// </summary>
    /// <param name="culture">目标语言。</param>
    /// <param name="options">回退与缺失键策略。</param>
    /// <param name="overlay">优先级最高的覆盖层（调试用本地语言文件）。</param>
    public Dictionary<string, string> BuildTable(
        string culture,
        LocalizationOptions options,
        IReadOnlyDictionary<string, string>? overlay = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var chain = options.ResolveChain(culture);
        var layers = new List<IReadOnlyDictionary<string, string>>(chain.Count);
        foreach (var item in chain)
            layers.Add(Get(item));

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layer in layers)
            keys.UnionWith(layer.Keys);
        if (overlay is not null)
            keys.UnionWith(overlay.Keys);

        var table = new Dictionary<string, string>(keys.Count, StringComparer.Ordinal);
        foreach (var key in keys)
        {
            var value = Resolve(key, layers, overlay, out var translated);
            if (value is null)
            {
                // 整条回退链都没有这个键：兜底为键名本身。
                value = key;
                translated = false;
            }

            table[key] = options.FormatMissing(key, value, translated);
        }

        return table;
    }

    /// <summary>统计某门语言在目标语中缺失（需要回退）的键。</summary>
    public IReadOnlyList<string> GetMissingKeys(string culture, LocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var own = Get(culture);
        var chain = options.ResolveChain(culture);
        var missing = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in chain)
        {
            foreach (var key in Get(item).Keys)
            {
                if (!own.ContainsKey(key))
                    missing.Add(key);
            }
        }

        var result = new List<string>(missing);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static string? Resolve(
        string key,
        List<IReadOnlyDictionary<string, string>> layers,
        IReadOnlyDictionary<string, string>? overlay,
        out bool translated)
    {
        translated = false;

        if (overlay is not null && overlay.TryGetValue(key, out var overlaid))
        {
            translated = true;
            return overlaid;
        }

        if (layers.Count > 0 && layers[0].TryGetValue(key, out var own))
        {
            translated = true;
            return own;
        }

        for (var i = 1; i < layers.Count; i++)
        {
            if (layers[i].TryGetValue(key, out var fallback))
                return fallback;
        }

        return null;
    }
}
