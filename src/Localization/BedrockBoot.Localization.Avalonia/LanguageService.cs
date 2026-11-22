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
using Avalonia;
using Avalonia.Threading;

namespace BedrockBoot.Localization.Avalonia;

/// <summary>
///     Avalonia 侧的语言服务：把 <see cref="LocalizationContext" /> 的键表写入应用资源，
///     并负责调试语言文件的热重载。
/// </summary>
/// <remarks>
///     语言状态、键表构建、查串全在核心库；本类只做两件事——
///     ① 把表批量推给 <c>Application.Resources</c>（<see cref="ResourceDictionaryBridge" />）；
///     ② 监听本地语言文件变化并更新覆盖层。
///     非 UI 程序集应当直接用 <see cref="Localizer" />，不经过这里。
/// </remarks>
public sealed class LanguageService
{
    private readonly ResourceDictionaryBridge _bridge = new();
    private readonly LocalizationContext _context = LocalizationContext.Current;
    private readonly object _watcherSync = new();

    private FileSystemWatcher? _watcher;
    private string? _watchedPath;
    private bool _started;

    public LanguageService()
    {
        _context.Changed += OnTableChanged;
    }

    /// <summary>进程级单例。</summary>
    public static LanguageService Current { get; } = new();

    /// <summary>底层状态，供需要直接访问来源 / 语言表的场景使用。</summary>
    public LocalizationContext Context => _context;

    /// <summary>所有字符串来源的目录。</summary>
    public StringCatalog Catalog => _context.Catalog;

    /// <summary>已注册的语言列表。</summary>
    public CultureRegistry Cultures => _context.Cultures;

    /// <summary>回退与缺失键策略，可在 <see cref="Start" /> 之前替换。</summary>
    public LocalizationOptions Options
    {
        get => _context.Options;
        set => _context.Options = value;
    }

    /// <summary>当前语言代码。</summary>
    public string Culture => _context.Culture;

    /// <summary>当前语言对应的 <see cref="System.Globalization.CultureInfo" />。</summary>
    public CultureInfo CultureInfo => _context.CultureInfo;

    /// <summary>当前生效的完整键表（键集合与语言无关）。</summary>
    public IReadOnlyDictionary<string, string> Table => _context.Table;

    /// <summary>是否已经挂到应用资源上。</summary>
    public bool IsStarted => _started;

    /// <summary>语言切换完成（表已写入资源字典）后触发。</summary>
    public event EventHandler<string>? CultureChanged
    {
        add => _context.CultureChanged += value;
        remove => _context.CultureChanged -= value;
    }

    /// <summary>查串；缺失时返回键名。</summary>
    public string this[string key] => _context.Get(key);

    /// <summary>
    ///     启动服务并把键表写入应用资源。可重复调用，第二次起等价于切换语言。
    /// </summary>
    public void Start(Application application, string culture)
    {
        ArgumentNullException.ThrowIfNull(application);

        _bridge.Attach(application);
        _started = true;
        _context.SetCulture(culture);
    }

    /// <summary>注册一个字符串来源。启动后注册会立即重建键表。</summary>
    public void Register(IStringSource source, bool apply = true) => _context.Register(source, apply);

    /// <summary>按 <c>avares://</c> 目录注册一个程序集的语言文件。</summary>
    public void RegisterModule(string id, string baseUri, bool apply = true) =>
        Register(new ResourceStringModule(id, baseUri), apply);

    /// <summary>切换语言；未注册的语言会退回基准语。</summary>
    public void SetCulture(string culture) => _context.SetCulture(culture);

    /// <summary>查串并格式化。格式化使用当前语言的 <see cref="CultureInfo" />。</summary>
    public string Get(string key, params object?[]? args) => _context.Get(key, args);

    /// <summary>列出当前语言需要回退的键（尚未翻译）。</summary>
    public IReadOnlyList<string> GetMissingKeys() => _context.GetMissingKeys();

    /// <summary>用本地语言文件覆盖当前语言，并开启热重载。</summary>
    public void DebugLanguage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(@"路径不能为空", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("语言文件不存在", fullPath);

        _context.SetOverlay(LoadOverlay(fullPath));
        StartWatcher(fullPath);
    }

    /// <summary>结束调试语言，恢复常规语言。</summary>
    public void StopDebugLanguage()
    {
        StopWatcher();
        _context.SetOverlay(null);
    }

    /// <summary>重建键表（来源或语言文件变化后调用）。</summary>
    public void Reload() => _context.Reload();

    private void OnTableChanged(object? sender, LocalizationChangedEventArgs e)
    {
        if (!_started)
            return;

        if (Dispatcher.UIThread.CheckAccess())
            _bridge.Apply(e.Table);
        else
            Dispatcher.UIThread.Post(() => _bridge.Apply(e.Table));

        var overlay = _context.Overlay is null ? string.Empty : "（含调试覆盖层）";
        LocalizationLog.Info(
            $"语言 {e.Culture} 生效{overlay}：{e.Table.Count} 键，其中 {_context.GetMissingKeys().Count} 个走回退");
    }

    private static IReadOnlyDictionary<string, string>? LoadOverlay(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ResourceStringModule.Parse(stream);
    }

    private void StartWatcher(string path)
    {
        StopWatcher();

        var directory = Path.GetDirectoryName(path)!;
        var fileName = Path.GetFileName(path);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.Size
                           | NotifyFilters.FileName
                           | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnWatchedFileChanged;
        _watcher.Created += OnWatchedFileChanged;
        _watcher.Renamed += OnWatchedFileChanged;
        _watchedPath = path;

        LocalizationLog.Info($"调试语言热重载已启用：{path}");
    }

    private void StopWatcher()
    {
        lock (_watcherSync)
        {
            if (_watcher is null)
                return;

            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnWatchedFileChanged;
            _watcher.Created -= OnWatchedFileChanged;
            _watcher.Renamed -= OnWatchedFileChanged;
            _watcher.Dispose();
            _watcher = null;
            _watchedPath = null;
        }
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        var path = _watchedPath;
        if (path is null)
            return;

        lock (_watcherSync)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    var overlay = LoadOverlay(path);
                    if (overlay is null)
                    {
                        LocalizationLog.Warn("热重载失败：根节点不是 ResourceDictionary");
                        return;
                    }

                    if (Dispatcher.UIThread.CheckAccess())
                        _context.SetOverlay(overlay);
                    else
                        Dispatcher.UIThread.Post(() => _context.SetOverlay(overlay));

                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    LocalizationLog.Warn($"热重载失败：{ex.Message}");
                    return;
                }
            }

            LocalizationLog.Warn("热重载失败：文件被占用");
        }
    }
}
