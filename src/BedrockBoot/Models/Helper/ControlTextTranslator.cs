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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     直接在已渲染的控件树里翻译文本：收集所有 TextBlock 的纯文本 Run，
///     批量送翻后只回写 Run.Text。控件结构与样式（字体、颜色、边距、链接、图片）
///     全程不碰，译文天然沿用原文排版；原文本保留在 Run 上用于还原。
/// </summary>
public static class ControlTextTranslator
{
    private static readonly Regex HasLetterRegex = new(@"\p{L}", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    ///     翻译控件树里的全部可见文本。
    ///     返回是否至少回写了一条译文（失败或无可译内容返回 false）。
    /// </summary>
    /// <param name="roots">控件树根（描述页的各个容器）</param>
    /// <param name="translateBatch">批量翻译委托：入参为原文列表，出参与入参一一对应，失败返回 null</param>
    /// <param name="progress">翻译进度回调（已处理条数 / 总条数）</param>
    public static async Task<bool> TranslateControlsAsync(
        IEnumerable<AvaloniaObject> roots,
        Func<IReadOnlyList<string>, Task<IReadOnlyList<string>?>> translateBatch,
        Action<int, int>? progress = null)
    {
        var runs = CollectRuns(roots);
        if (runs.Count == 0) return false;

        // 段落内的空白折叠过一次（HTML 源码缩进 → 单空格），翻译前同样收敛，
        // 保证控件树里读出的文本与渲染时展示的一致
        var originals = runs.Select(r => NormalizeForTranslate(r.Text)).ToList();

        var translatableIndexes = new List<int>();
        for (var i = 0; i < originals.Count; i++)
            if (!string.IsNullOrWhiteSpace(originals[i]) && HasLetterRegex.IsMatch(originals[i]))
                translatableIndexes.Add(i);

        if (translatableIndexes.Count == 0) return false;

        progress?.Invoke(0, translatableIndexes.Count);

        var texts = translatableIndexes.Select(i => originals[i]).ToList();
        var results = await translateBatch(texts);
        if (results == null || results.Count != texts.Count) return false;

        var applied = 0;
        for (var i = 0; i < translatableIndexes.Count; i++)
        {
            var original = originals[translatableIndexes[i]];
            var translated = results[i];

            // 译文为空或与原文相同（接口对不可译内容原样返回）视为未翻译
            if (string.IsNullOrWhiteSpace(translated) ||
                string.Equals(translated, original, StringComparison.Ordinal))
                continue;

            var run = runs[translatableIndexes[i]];
            run.Text = translated;
            applied++;
        }

        progress?.Invoke(translatableIndexes.Count, translatableIndexes.Count);
        return applied > 0;
    }

    /// <summary>深度优先收集控件树里所有带文本的 Run，保持文档顺序。</summary>
    private static List<Run> CollectRuns(IEnumerable<AvaloniaObject> roots)
    {
        var runs = new List<Run>();

        void Walk(AvaloniaObject? obj)
        {
            switch (obj)
            {
                case TextBlock textBlock:
                    // 直接文本不走 Inlines
                    if (textBlock.Inlines == null)
                    {
                        if (!string.IsNullOrEmpty(textBlock.Text))
                            textBlock.Inlines = new InlineCollection { new Run(textBlock.Text) };
                        else
                            break;
                    }

                    foreach (var inline in textBlock.Inlines)
                        if (inline is Run run) runs.Add(run);
                    break;

                case ILogical logical:
                    foreach (var child in logical.LogicalChildren)
                        Walk(child as AvaloniaObject);
                    break;

                case AvaloniaObject control:
                    // InlineUIContainer/HyperlinkButton 的 Content 不一定在逻辑树里可达，
                    // 反射兜底读 Content，保证嵌在链接里的文字也被翻译
                    if (control is ContentControl { Content: AvaloniaObject content }) Walk(content);
                    break;
            }
        }

        foreach (var root in roots) Walk(root);
        return runs;
    }

    /// <summary>与渲染层一致的空白折叠，译文与原文在同一空白规范下比较。</summary>
    private static string NormalizeForTranslate(string? text) =>
        text == null ? string.Empty : WhitespaceRegex.Replace(text, " ").Trim();
}
