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

using System.Reflection;
using System.Xml.Linq;

namespace BedrockBoot.Localization;

/// <summary>
///     从一个程序集内嵌的语言目录加载键值，约定文件名为 <c>{baseUri}/{culture}.axaml</c>。
/// </summary>
/// <remarks>
///     语言文件以 <c>EmbeddedResource</c> 打包，由 <see cref="Parse" /> 直接读 XML，
///     既不经过 Avalonia 的 XAML 引擎，也不经过 <c>avares://</c> 的程序集解析。
///     这样可以避开三条限制：无 <c>x:Class</c> 的 <c>ResourceDictionary</c> 无法通过
///     <c>avares://</c> 取到流、程序集首次解析失败会被负缓存污染，以及
///     <c>PublishTrimmed</c> 下运行时 XAML 加载器不可用。
/// </remarks>
/// <remarks>
///     本类型不依赖任何 UI 框架，因此非 UI 程序集（下载器、平台层）也能直接查串。
///     每个自带文案的程序集只需放一份语言目录并注册一个实例；本仓库默认把
///     全部文案集中在 <c>BedrockBoot.Localization/Resources/I18n</c>，只注册一个实例。
/// </remarks>
public sealed class ResourceStringModule : IStringSource
{
    /// <summary>语言文件的扩展名。</summary>
    public const string FileExtension = ".axaml";

    private static readonly XName XamlKey = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    private readonly string _assemblyName;
    private readonly string _folder;
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>?> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _sync = new();

    /// <param name="id">来源标识，仅用于日志。</param>
    /// <param name="baseUri">语言目录，如 <c>avares://BedrockBoot.Localization/Resources/I18n</c>。</param>
    public ResourceStringModule(string id, string baseUri)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(@"来源标识不能为空", nameof(id));
        if (string.IsNullOrWhiteSpace(baseUri))
            throw new ArgumentException(@"语言目录不能为空", nameof(baseUri));

        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var uri) || uri.Scheme != "avares")
            throw new ArgumentException(@"语言目录必须是 avares:// 形式", nameof(baseUri));

        Id = id;
        _assemblyName = uri.Host;
        _folder = uri.AbsolutePath.Trim('/').Replace('/', '.');
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Load(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        lock (_sync)
        {
            if (_cache.TryGetValue(culture, out var cached))
                return cached;
        }

        var loaded = LoadCore(culture);

        lock (_sync)
        {
            _cache[culture] = loaded;
        }

        return loaded;
    }

    /// <summary>丢弃缓存，下次加载时重新读取资源。</summary>
    public void Invalidate()
    {
        lock (_sync)
        {
            _cache.Clear();
        }
    }

    /// <summary>把 <c>ResourceDictionary</c> 形式的 axaml 解析成纯字符串键值。</summary>
    /// <remarks>
    ///     只取带 <c>x:Key</c> 的元素的文本内容；语言文件里除 <c>x:String</c> 之外没有别的元素。
    ///     CDATA 与实体会被 <see cref="XDocument" /> 正常还原。
    /// </remarks>
    public static Dictionary<string, string> Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var element in document.Descendants())
        {
            var key = element.Attribute(XamlKey)?.Value;
            if (!string.IsNullOrEmpty(key))
                result[key] = element.Value;
        }

        return result;
    }

    /// <summary>从程序集里读取一门语言的键值；该程序集没有这门语言时返回 <c>null</c>。</summary>
    /// <param name="assembly">已加载的程序集。</param>
    /// <param name="folder">语言目录（相对项目根），如 <c>I18n</c>；允许多级，用 <c>/</c> 分隔。</param>
    /// <param name="culture">语言代码，如 <c>zh_CN</c>。</param>
    public static Dictionary<string, string>? LoadFrom(Assembly assembly, string folder, string culture)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        var prefix = string.IsNullOrWhiteSpace(folder) ? string.Empty : "." + folder.Replace('/', '.');
        var suffix = $"{prefix}.{culture}{FileExtension}";

        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (name is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(name);
        return stream is null ? null : Parse(stream);
    }

    private IReadOnlyDictionary<string, string>? LoadCore(string culture)
    {
        try
        {
            var assembly = ResolveAssembly(_assemblyName);
            if (assembly is null)
            {
                LocalizationLog.Warn($"模块 {Id} 找不到程序集 {_assemblyName}");
                return null;
            }

            var loaded = LoadFrom(assembly, _folder, culture);
            if (loaded is null)
                LocalizationLog.Info($"模块 {Id} 没有 {culture} 语言文件（{_assemblyName}:{_folder}）");

            return loaded;
        }
        catch (Exception ex)
        {
            LocalizationLog.Warn($"模块 {Id} 加载 {culture} 失败：{ex.GetType().Name} {ex.Message}");
            return null;
        }
    }

    private static Assembly? ResolveAssembly(string name)
    {
        foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(loaded.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch
        {
            return null;
        }
    }
}
