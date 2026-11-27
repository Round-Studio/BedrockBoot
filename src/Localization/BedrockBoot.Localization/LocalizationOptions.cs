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
///     回退与缺失键策略。默认值来自 <see cref="CultureManifest.Builtin" />，
///     因此"基准语是哪个、外语兜底到哪个、谁回退到谁"只在 <c>cultures.json</c> 里写一次。
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>键集合的权威来源，同时也是最终兜底语言。</summary>
    public string BaselineCulture { get; init; } = CultureManifest.Builtin.Baseline;

    /// <summary>外语的统一兜底语言。</summary>
    public string FallbackCulture { get; init; } = CultureManifest.Builtin.Fallback;

    /// <summary>
    ///     语种内部的回退目标，如繁体与粤语回退到简体；由清单里的 <c>parent</c> 生成。
    /// </summary>
    public IReadOnlyDictionary<string, string> CultureFallbacks { get; init; } =
        CultureManifest.Builtin.BuildFallbacks();

    /// <summary>
    ///     未翻译（值来自回退链）时的显示策略。默认静默，终端用户不会看到标记。
    /// </summary>
    public MissingKeyStyle MissingKeyStyle { get; init; } = MissingKeyStyle.Silent;

    /// <summary>未翻译值的左标记。</summary>
    public string MissingPrefix { get; init; } = "\u27E6";

    /// <summary>未翻译值的右标记。</summary>
    public string MissingSuffix { get; init; } = "\u27E7";

    /// <summary>
    ///     解析某门语言的完整回退链（去重、保序）。
    ///     目标语 → 语种内部基准 → 外语兜底 → 键集合基准语。
    /// </summary>
    public IReadOnlyList<string> ResolveChain(string culture)
    {
        var chain = new List<string>(4);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Push(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                chain.Add(value);
        }

        Push(culture);
        if (CultureFallbacks.TryGetValue(culture, out var cultureFallback))
            Push(cultureFallback);
        Push(FallbackCulture);
        Push(BaselineCulture);

        return chain;
    }

    /// <summary>包裹未翻译的值。</summary>
    public string Mark(string value) => $"{MissingPrefix}{value}{MissingSuffix}";

    /// <summary>
    ///     按 <see cref="MissingKeyStyle" /> 决定一个键最终显示什么。
    /// </summary>
    /// <param name="key">键名。</param>
    /// <param name="value">回退链求值结果；键完全不存在时等于键名。</param>
    /// <param name="translated">该值是否来自当前语言本身（或调试覆盖层）。</param>
    public string FormatMissing(string key, string value, bool translated)
    {
        if (translated)
            return value;

        return MissingKeyStyle switch
        {
            MissingKeyStyle.MarkValue => Mark(value),
            MissingKeyStyle.ShowKey => Mark(key),
            _ => value,
        };
    }

    /// <summary>派生一个只改缺失键策略的新实例（init 属性无法就地修改）。</summary>
    public LocalizationOptions WithMissingKeyStyle(MissingKeyStyle style) => new()
    {
        BaselineCulture = BaselineCulture,
        FallbackCulture = FallbackCulture,
        CultureFallbacks = CultureFallbacks,
        MissingKeyStyle = style,
        MissingPrefix = MissingPrefix,
        MissingSuffix = MissingSuffix,
    };
}
