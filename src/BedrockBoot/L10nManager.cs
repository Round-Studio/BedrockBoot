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

public class L10nManager : INotifyPropertyChanged
{
    public static L10nManager Instance { get; } = new();

    // 索引器：让 XAML 可以通过 {Binding [Key]} 访问
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

        // 3. 通知所有绑定了索引器的 UI 更新
        OnPropertyChanged("Item[]"); 
    }

    private string GetString(string key)
    {
        if (Application.Current!.Resources.TryGetResource(key, null, out var value) && value is string s)
            return s;
        return $"#{key}#"; // 找不到键时返回占位符
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