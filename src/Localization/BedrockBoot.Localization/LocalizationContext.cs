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

using System.Globalization;

namespace BedrockBoot.Localization;

/// <summary>键表发生变化（语言切换、新增来源、热重载）时的事件参数。</summary>
public sealed class LocalizationChangedEventArgs : EventArgs
{
    /// <param name="culture">变化后生效的语言代码。</param>
    /// <param name="table">变化后的完整键表。</param>
    public LocalizationChangedEventArgs(string culture, IReadOnlyDictionary<string, string> table)
    {
        Culture = culture;
        Table = table;
    }

    /// <summary>变化后生效的语言代码。</summary>
    public string Culture { get; }

    /// <summary>变化后的完整键表，键集合与语言无关。</summary>
    public IReadOnlyDictionary<string, string> Table { get; }
}

/// <summary>
///     本地化的运行时状态：当前语言、当前键表、字符串来源，以及查串本身。
/// </summary>
/// <remarks>
///     这里刻意不依赖任何 UI 框架——只有"当前键表是什么"和"怎么取一个值"，
///     至于把表写进 <c>Application.Resources</c>、在 UI 线程上刷新，是 Avalonia 层的事
///     （见 <c>BedrockBoot.Localization.Avalonia.LanguageService</c>）。
///     这样下载器、平台层这类非 UI 程序集也能直接 <see cref="Get" />，不必引用 UI 框架。
///     <para>
///         <b>零初始化</b>：构造时自动登记内置语言清单与内置语言文件来源，
///         因此 <c>Localizer.Get(...)</c> 在任何宿主里都能直接用，不需要宿主先调注册方法。
///         外部模块仍可随时 <see cref="Register" /> 追加来源，或往
///         <see cref="Cultures" /> 里加清单之外的语言。
///     </para>
/// </remarks>
public sealed class LocalizationContext
{
    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(0);

    /// <summary>进程级单例。</summary>
    public static LocalizationContext Current { get; } = new();

    /// <summary>所有字符串来源的目录。</summary>
    public StringCatalog Catalog { get; } = new();

    /// <summary>已注册的语言列表。</summary>
    public CultureRegistry Cultures { get; } = new();

    /// <summary>回退与缺失键策略，可在首次切换语言之前替换。</summary>
    public LocalizationOptions Options { get; set; } = new();

    /// <summary>当前语言代码；尚未设置过语言时为空字符串。</summary>
    public string Culture { get; private set; } = string.Empty;

    /// <summary>当前语言对应的 <see cref="System.Globalization.CultureInfo" />。</summary>
    public CultureInfo CultureInfo { get; private set; } = CultureInfo.InvariantCulture;

    /// <summary>当前生效的完整键表（键集合与语言无关）。</summary>
    public IReadOnlyDictionary<string, string> Table { get; private set; } = Empty;

    /// <summary>调试用的覆盖层（本地语言文件），优先级高于所有来源。</summary>
    public IReadOnlyDictionary<string, string>? Overlay { get; private set; }

    /// <summary>是否已经选定过语言。</summary>
    public bool IsReady => Culture.Length > 0;

    /// <summary>
    ///     构造时把内置语言清单与内置语言文件来源装好，使查串无需任何显式初始化。
    /// </summary>
    /// <remarks>
    ///     这里是<b>全函数</b>：资源缺失或清单损坏只会让内置来源为空表 / 退回默认清单，
    ///     不能抛——本类型经静态属性 <see cref="Current" /> 初始化，抛出去就是启动即崩。
    /// </remarks>
    public LocalizationContext()
    {
        try
        {
            CultureManifest.Builtin.RegisterInto(Cultures);
            BuiltinStrings.RegisterInto(this);
        }
        catch (Exception ex)
        {
            LocalizationLog.Warn($"内置语言初始化失败（{ex.GetType().Name}：{ex.Message}）");
        }
    }

    /// <summary>键表被重建后触发，UI 层据此刷新资源字典。</summary>
    public event EventHandler<LocalizationChangedEventArgs>? Changed;

    /// <summary>语言切换完成（键表已就绪）后触发。</summary>
    public event EventHandler<string>? CultureChanged;

    /// <summary>查串；缺失时返回键名。</summary>
    public string this[string key] => Get(key);

    /// <summary>注册一个字符串来源。已就绪时会立即重建键表。</summary>
    /// <param name="source">字符串来源。</param>
    /// <param name="apply">是否立即重建键表；启动前注册可传 <c>false</c> 避免重复重建。</param>
    public void Register(IStringSource source, bool apply = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        Catalog.Add(source);

        if (apply && IsReady)
            Rebuild();
    }

    /// <summary>切换语言；未注册的语言会退回基准语。</summary>
    public void SetCulture(string culture)
    {
        if (!Cultures.Contains(culture))
        {
            LocalizationLog.Warn($"语言 {culture} 未注册，回退到 {Options.BaselineCulture}");
            culture = Options.BaselineCulture;
        }

        Culture = culture;
        CultureInfo = ResolveCultureInfo(culture);
        Rebuild();
        CultureChanged?.Invoke(this, culture);
    }

    /// <summary>设置或清除调试覆盖层（本地语言文件）。</summary>
    public void SetOverlay(IReadOnlyDictionary<string, string>? overlay)
    {
        Overlay = overlay;

        if (IsReady)
            Rebuild();
    }

    /// <summary>丢弃所有来源的缓存并重建键表。</summary>
    public void Reload()
    {
        Catalog.Invalidate();

        if (IsReady)
            Rebuild();
    }

    /// <summary>查串并格式化。格式化使用当前语言的 <see cref="CultureInfo" />。</summary>
    public string Get(string key, params object?[]? args)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!Table.TryGetValue(key, out var value))
            return key;

        if (args is null || args.Length == 0)
            return value;

        try
        {
            return string.Format(CultureInfo, value, args);
        }
        catch (FormatException)
        {
            // 译文里的占位符与传入参数不匹配：保留原文，避免界面崩溃。
            LocalizationLog.Warn($"键 {key} 的占位符与参数不匹配");
            return value;
        }
    }

    /// <summary>列出当前语言需要回退的键（尚未翻译）。</summary>
    public IReadOnlyList<string> GetMissingKeys() =>
        IsReady ? Catalog.GetMissingKeys(Culture, Options) : Array.Empty<string>();

    private void Rebuild()
    {
        var table = Catalog.BuildTable(Culture, Options, Overlay);
        Table = table;
        Changed?.Invoke(this, new LocalizationChangedEventArgs(Culture, table));
    }

    private static CultureInfo ResolveCultureInfo(string culture)
    {
        var name = culture.Replace('_', '-');
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            var parent = name.Split('-')[0];
            try
            {
                return CultureInfo.GetCultureInfo(parent);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
