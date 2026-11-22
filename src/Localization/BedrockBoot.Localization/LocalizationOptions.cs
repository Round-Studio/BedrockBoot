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

    /// <summary>为 true 时，未翻译的值会被包裹，便于一眼识别兜底内容。</summary>
    public bool MarkMissing { get; init; } = true;

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
}
