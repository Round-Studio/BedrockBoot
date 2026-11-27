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
///     键集合收缩（只有调试覆盖层才会造成）时再逐键移除，数量极少。
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

    /// <summary>用新表覆盖当前字典：批量 upsert 之后，再清掉新表里不存在的键。</summary>
    /// <remarks>
    ///     <see cref="ResourceDictionary.SetItems" /> 是 upsert，不会删除未列出的键。
    ///     常规切语言时键集合不变，因此没有多余键；但调试覆盖层（本地语言文件）可以带来
    ///     任意额外的键，覆盖层被替换或撤销后那些键会留在字典里，
    ///     Avalonia 仍会命中旧的调试值——所以这里必须补一次清理。
    /// </remarks>
    public void Apply(IReadOnlyDictionary<string, string> table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (_dictionary is null)
            return;

        var pairs = new KeyValuePair<object, object?>[table.Count];
        var index = 0;
        foreach (var pair in table)
            pairs[index++] = new KeyValuePair<object, object?>(pair.Key, pair.Value);

        _dictionary.SetItems(pairs);

        // 每个 Remove 都会单独发通知，但多出来的键只可能是"覆盖层里有、当前表里没有"
        // 的那几个；键集合没收缩时这里一次都不会进入。
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
