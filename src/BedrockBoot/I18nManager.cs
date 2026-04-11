using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using BedrockBoot.Base.Enum.Language;

public class I18nManager : INotifyPropertyChanged
{
    public static I18nManager Instance { get; } = new();

    public string this[string key] => GetString(key);

    private ResourceDictionary? _currentLanguageDict;

    public void SystemLanguage(LanguageEnum language)
    {
        var uri = new Uri(LanguageEnumExtensions.GetUri(language));
        var dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);

        var appResources = Application.Current!.Resources.MergedDictionaries;
        if (_currentLanguageDict != null) appResources.Remove(_currentLanguageDict);
        
        appResources.Add(dict);
        _currentLanguageDict = dict;

        OnPropertyChanged($"Item[]"); 
        Console.WriteLine($@"SystemLanguage: {Instance["LanguageName"]}");
    }

    private string GetString(string key)
    {
        if (Application.Current!.Resources.TryGetResource(key, null, out var value) && value is string s)
            return s;
        return $"{key}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    
    public static class LanguageEnumExtensions
    {
        public static string GetUri(LanguageEnum language)
        {
            var field = language.GetType().GetField(language.ToString());
            var attribute = field?.GetCustomAttribute<LanguageResourceAttribute>();
            return attribute?.Uri ?? string.Empty;
        }
    }
}