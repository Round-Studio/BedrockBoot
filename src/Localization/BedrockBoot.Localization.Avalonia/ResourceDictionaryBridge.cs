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

using Avalonia;
using Avalonia.Controls;

namespace BedrockBoot.Localization.Avalonia;

/// <summary>
///     维护一个常驻的资源字典，并把完整键表批量写入其中。
/// </summary>
/// <remarks>
///     写入走 <see cref="ResourceDictionary.SetItems" />：批量 upsert 只触发一次
///     <c>ResourcesChanged</c>，界面不会闪烁，也不会出现键缺失的中间状态。
///     逐键赋值会触发同等数量的通知（实测 2000 键 = 2000 次），因此禁止那样写。
/// </remarks>
internal sealed class ResourceDictionaryBridge
{
    private ResourceDictionary? _dictionary;

    /// <summary>挂到应用资源上；重复调用无副作用。</summary>
    public void Attach(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (_dictionary is not null)
            return;

        _dictionary = new ResourceDictionary();
        application.Resources.MergedDictionaries.Add(_dictionary);
    }

    /// <summary>用新表覆盖当前字典（只通知一次）。</summary>
    public void Apply(IReadOnlyDictionary<string, string> table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (_dictionary is null)
            return;

        var pairs = new KeyValuePair<object, object?>[table.Count];
        var index = 0;
        foreach (var pair in table)
            pairs[index++] = new KeyValuePair<object, object?>(pair.Key, pair.Value);

        // SetItems 语义是 upsert：不会删除未列出的键，漏传的键会保留旧值而不是变空。
        _dictionary.SetItems(pairs);
    }

    /// <summary>移除多出来的键（仅键集合收缩时需要）。</summary>
    public void RemoveMissing(IReadOnlyDictionary<string, string> table)
    {
        if (_dictionary is null)
            return;

        var stale = new List<object>();
        foreach (var key in _dictionary.Keys)
        {
            if (key is string name && !table.ContainsKey(name))
                stale.Add(key);
        }

        foreach (var key in stale)
            _dictionary.Remove(key);
    }

    /// <summary>当前字典的键数量。</summary>
    public int Count => _dictionary?.Count ?? 0;
}
