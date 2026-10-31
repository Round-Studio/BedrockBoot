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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot;

public class I18nManager : INotifyPropertyChanged
{
    private ResourceDictionary? _currentLanguageDict;
    private FileSystemWatcher? _watcher;
    private string? _watchedPath;
    private readonly object _reloadLock = new();

    public class LanguageItem
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }
    }

    public static Dictionary<string, LanguageItem> LanguageDict { get; private set; } = new();
    public static I18nManager Instance { get; } = new();

    public string this[string key] => GetString(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SystemLanguage(string key)
    {
        if (!LanguageDict.TryGetValue(key, out var info))
        {
            info = LanguageDict["zh_CN"];
        }

        var url = info.Code ?? "zh_CN";
        SystemLanguageUri(new Uri(url));
    }

    public void SystemLanguageUri(Uri uri)
    {
        ResourceDictionary dict;

        if (uri.IsFile)
        {
            using var stream = File.OpenRead(uri.LocalPath);
            var loaded = AvaloniaRuntimeXamlLoader.Load(stream);
            dict = loaded as ResourceDictionary
                   ?? throw new InvalidOperationException("本地文件加载结果不是 ResourceDictionary");
        }
        else
        {
            dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
        }

        var appResources = Application.Current!.Resources.MergedDictionaries;
        if (_currentLanguageDict != null) appResources.Remove(_currentLanguageDict);

        appResources.Add(dict);
        _currentLanguageDict = dict;

        OnPropertyChanged("Item[]");
        Console.WriteLine($@"SystemLanguage: {Instance["LanguageName"]}");
    }

    public void DebugLanguage(string pathOrUri)
    {
        var uri = pathOrUri.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? new Uri(pathOrUri)
            : new Uri(Path.GetFullPath(pathOrUri));

        if (!uri.IsFile)
            throw new ArgumentException("调试语言仅支持本地文件路径或 file:// URI", nameof(pathOrUri));

        var localPath = uri.LocalPath;
        if (!File.Exists(localPath))
            throw new FileNotFoundException("语言文件不存在", localPath);

        StopWatcher();
        SystemLanguageUri(uri);
        StartWatcher(localPath);
    }

    private void StartWatcher(string localPath)
    {
        var directory = Path.GetDirectoryName(localPath)!;
        var fileName = Path.GetFileName(localPath);

        _watchedPath = localPath;
        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite
                         | NotifyFilters.Size
                         | NotifyFilters.FileName
                         | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnWatchedFileChanged;
        _watcher.Created += OnWatchedFileChanged;
        _watcher.Renamed += OnWatchedFileChanged;

        Console.WriteLine($@"[I18n] 热重载已启用: {localPath}");
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_watchedPath is null)
            return;

        lock (_reloadLock)
        {
            var retries = 5;
            while (retries-- > 0)
            {
                try
                {
                    using var stream = new FileStream(
                        _watchedPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);

                    var loaded = AvaloniaRuntimeXamlLoader.Load(stream);
                    if (loaded is not ResourceDictionary dict)
                    {
                        Console.WriteLine(@"[I18n] 热重载失败: 文件根节点不是 ResourceDictionary");
                        return;
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var appResources = Application.Current!.Resources.MergedDictionaries;
                        if (_currentLanguageDict != null) appResources.Remove(_currentLanguageDict);
                        appResources.Add(dict);
                        _currentLanguageDict = dict;

                        OnPropertyChanged("Item[]");
                        Console.WriteLine($@"[I18n] 热重载完成: {Instance["LanguageName"]}");
                    });

                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"[I18n] 热重载失败: {ex.Message}");
                    return;
                }
            }

            Console.WriteLine(@"[I18n] 热重载失败: 文件被占用");
        }
    }

    private void StopWatcher()
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

    public static void AddLanguage()
    {
        LanguageRegister("简体中文（中国大陆）", "zh_CN", "avares://BedrockBoot/I18n/zh_CN.axaml");
        LanguageRegister("粵語（中國廣東）", "yue_HK", "avares://BedrockBoot/I18n/yue_HK.axaml");
        LanguageRegister("English (US)", "en_US", "avares://BedrockBoot/I18n/en_US.axaml");
        LanguageRegister("Japanese", "ja_JP", "avares://BedrockBoot/I18n/ja_JP.axaml");
    }

    private string GetString(string key)
    {
        if (Application.Current!.Resources.TryGetResource(key, null, out var value) && value is string s)
            return s;

        Console.WriteLine($@"未翻译键值 {key}");
        return $"{key}";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static void LanguageRegister(string languageName, string key, string uri)
    {
        Console.WriteLine($"注册语言 {key} - {languageName}");
        LanguageDict.Add(key, new LanguageItem()
        {
            Code = uri,
            DisplayName = languageName
        });
    }
}