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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using BedrockBoot.Localization;
using BedrockBoot.Localization.Avalonia;

namespace BedrockBoot;

/// <summary>
///     兼容门面：保留既有调用点（<c>Instance["Key"]</c>），实现委托给
///     <see cref="BedrockBoot.Localization.LocalizationContext" /> 与
///     <see cref="BedrockBoot.Localization.Avalonia.LanguageService" />。
/// </summary>
/// <remarks>
///     新代码请直接使用 <see cref="BedrockBoot.Localization.Localizer" />（核心库，UI 框架无关），
///     这样库项目（BedrockBoot.Core / Windows / Linux 等）无需引用主程序即可查串。
/// </remarks>
public class I18nManager : INotifyPropertyChanged
{
    /// <summary>清单异常时的兜底语言。</summary>
    private static string DefaultCulture => LanguageService.Current.Options.BaselineCulture;

    /// <summary>一门语言的注册项。</summary>
    public class LanguageItem
    {
        /// <summary>在语言选择器中显示的文本。</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>语言代码，如 <c>zh_CN</c>。</summary>
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>已注册语言，按 <see cref="AddLanguage" /> 的注册顺序排列。</summary>
    public static Dictionary<string, LanguageItem> LanguageDict { get; } = new();

    /// <summary>进程级单例，仅用于保持旧的调用写法。</summary>
    public static I18nManager Instance { get; } = new();

    /// <summary>查串，缺失时返回键名。</summary>
    public string this[string key] => Localizer.Get(key);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>切换语言。</summary>
    public void SystemLanguage(string key)
    {
        var culture = !string.IsNullOrEmpty(key) && LanguageDict.ContainsKey(key) ? key : DefaultCulture;
        var service = LanguageService.Current;

        // 切到常规语言就意味着退出调试预览：覆盖层的优先级高于所有来源，
        // 不清掉的话选完语言界面还是预览文件里的旧文案。
        if (service.IsDebugging)
            service.StopDebugLanguage();

        if (!service.Cultures.Contains(culture))
        {
            var displayName = LanguageDict.TryGetValue(culture, out var item) ? item.DisplayName : culture;
            service.Cultures.Register(culture, displayName);
        }

        var application = Application.Current;
        if (application is null)
        {
            LocalizationLog.Warn($"切换语言 {culture} 失败：Application 尚未初始化");
            return;
        }

        if (service.IsStarted)
            service.SetCulture(culture);
        else
            service.Start(application, culture);

        OnPropertyChanged("Item[]");
    }

    /// <summary>以 URI 的形式指定语言。</summary>
    [Obsolete("改用 SystemLanguage(culture) 或 DebugLanguage(path)")]
    public void SystemLanguageUri(Uri uri)
    {
        if (uri.IsFile)
        {
            DebugLanguage(uri.LocalPath);
            return;
        }

        SystemLanguage(Path.GetFileNameWithoutExtension(uri.ToString()));
    }

    /// <summary>用本地语言文件预览并开启热重载。</summary>
    public void DebugLanguage(string pathOrUri)
    {
        var localPath = pathOrUri.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? new Uri(pathOrUri).LocalPath
            : Path.GetFullPath(pathOrUri);

        LanguageService.Current.DebugLanguage(localPath);
    }

    /// <summary>把语言清单搬成界面可绑定的结构，并订阅查串变化。</summary>
    /// <remarks>
    ///     语言清单已外置到 <c>BedrockBoot.Localization/Resources/I18n/cultures.json</c>，
    ///     文案来源也已由 <see cref="LocalizationContext" /> 自动注册；
    ///     因此本方法不再硬编码语言，也不需要注册任何 <c>avares</c> 目录——加一门语言
    ///     只需在清单里加一行并放一个 <c>{code}.axaml</c>。
    /// </remarks>
    public static void AddLanguage()
    {
        var service = LanguageService.Current;
        var cultures = service.Cultures;

        foreach (var entry in cultures.Entries)
        {
            LanguageDict[entry.Culture] = new LanguageItem
            {
                DisplayName = entry.DisplayName,
                Code = entry.Culture,
            };
        }

        if (LanguageDict.Count == 0)
        {
            // 清单损坏且兜底清单也失效时的最后一道防线：至少留一条可选。
            LocalizationLog.Warn("语言清单为空，退回基准语");
            LanguageRegister(cultures.GetDisplayName(DefaultCulture), DefaultCulture);
        }

        service.CultureChanged += (_, _) => Instance.OnPropertyChanged("Item[]");
    }

    /// <summary>登记一门清单之外的语言（供外部模块使用）。</summary>
    public static void LanguageRegister(string languageName, string key)
    {
        LanguageService.Current.Cultures.Register(key, languageName);
        LanguageDict[key] = new LanguageItem
        {
            DisplayName = languageName,
            Code = key,
        };
    }

    /// <summary>触发属性变更通知。</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
