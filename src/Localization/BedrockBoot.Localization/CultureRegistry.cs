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
///     已注册语言的登记表。与 UI 框架无关，按注册顺序保存。
/// </summary>
public sealed class CultureRegistry
{
    private readonly List<CultureEntry> _entries = new();
    private readonly Dictionary<string, CultureEntry> _byCode = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>按注册顺序返回全部语言。</summary>
    public IReadOnlyList<CultureEntry> Entries => _entries;

    /// <summary>注册一门语言；重复注册时更新显示名而不改变顺序。</summary>
    public void Register(string culture, string displayName)
    {
        if (string.IsNullOrWhiteSpace(culture))
            throw new ArgumentException(@"区域代码不能为空", nameof(culture));

        if (_byCode.TryGetValue(culture, out var existing))
        {
            var index = _entries.IndexOf(existing);
            var updated = existing with { DisplayName = displayName };
            _byCode[culture] = updated;
            _entries[index] = updated;
            return;
        }

        var entry = new CultureEntry(culture, displayName);
        _byCode[culture] = entry;
        _entries.Add(entry);
    }

    /// <summary>判断某门语言是否已注册。</summary>
    public bool Contains(string? culture) =>
        !string.IsNullOrWhiteSpace(culture) && _byCode.ContainsKey(culture);

    /// <summary>按代码查找语言。</summary>
    public CultureEntry? Find(string? culture) =>
        !string.IsNullOrWhiteSpace(culture) && _byCode.TryGetValue(culture, out var entry) ? entry : null;

    /// <summary>取显示名，找不到时返回原代码。</summary>
    public string GetDisplayName(string culture) => Find(culture)?.DisplayName ?? culture;
}
