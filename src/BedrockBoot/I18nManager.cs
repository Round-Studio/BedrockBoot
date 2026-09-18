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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Enum.Language;

public class I18nManager : INotifyPropertyChanged
{
    private ResourceDictionary? _currentLanguageDict;
    public static I18nManager Instance { get; } = new();

    public string this[string key] => GetString(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SystemLanguage(LanguageEnum language)
    {
        var uri = new Uri(LanguageEnumExtensions.GetUri(language));
        var dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);

        var appResources = Application.Current!.Resources.MergedDictionaries;
        if (_currentLanguageDict != null) appResources.Remove(_currentLanguageDict);

        appResources.Add(dict);
        _currentLanguageDict = dict;

        OnPropertyChanged("Item[]");
        Console.WriteLine($@"SystemLanguage: {Instance["LanguageName"]}");
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


    public static class LanguageEnumExtensions
    {
        private static readonly ConcurrentDictionary<string, string> _uriCache = new();

        public static string GetUri(LanguageEnum language)
        {
            var key = language.ToString();
            return _uriCache.GetOrAdd(key, static k =>
            {
                var field = typeof(LanguageEnum).GetField(k);
                var attribute = field?.GetCustomAttribute<LanguageResourceAttribute>();
                return attribute?.Uri ?? string.Empty;
            });
        }
    }
}