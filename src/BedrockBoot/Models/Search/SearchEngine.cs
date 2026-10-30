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
using System.Linq;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage.AboutPages;
using BedrockBoot.Views.Pages.SettingSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingAccountPages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingDownloadPages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPluginPages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using CoreGlobal = BedrockBoot.Core.Global.GlobalModel;
using UiGlobal = BedrockBoot.Models.Global.GlobalModel;

namespace BedrockBoot.Models.Search;

/// <summary>
///     全局搜索引擎：汇聚启动器页面、设置项、已安装实例与聚合搜索入口。
/// </summary>
public class SearchEngine
{
    private static readonly StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

    private static I18nManager i18n => I18nManager.Instance;

    public static SearchEngine Instance { get; } = new();

    // 实例缓存：避免每次按键都扫盘
    private List<VersionConfig>? _instanceCache;
    private DateTime _instanceCacheTime = DateTime.MinValue;

    private readonly List<SearchEntry> _entries;

    private class SearchEntry
    {
        public Func<string> Title { get; init; }
        public Func<string>? Description { get; init; }
        public Func<string> Category { get; init; }
        public string Glyph { get; init; } = "\uE713";
        public string Keywords { get; init; } = "";
        public Action Execute { get; init; }
    }

    private SearchEngine()
    {
        _entries = BuildEntries();
    }

    /// <summary>
    ///     执行一次全局搜索；query 为空时返回空列表。
    /// </summary>
    public List<SearchResult> Search(string query)
    {
        query = (query ?? "").Trim();

        var results = new List<SearchResult>();
        if (query.Length == 0) return results;

        foreach (var entry in _entries)
        {
            var title = entry.Title();
            var description = entry.Description?.Invoke() ?? "";
            var score = MatchScore(title, entry.Keywords, description, query);
            if (score <= 0) continue;

            results.Add(new SearchResult
            {
                Title = title,
                Description = string.IsNullOrEmpty(description) ? null : description,
                Category = entry.Category(),
                Glyph = entry.Glyph,
                Score = score,
                Execute = entry.Execute
            });
        }

        foreach (var version in GetInstances())
        {
            if (version.Info == null) continue;

            var name = version.Info.VersionName ?? "";
            var number = version.Info.Version ?? "";
            var description =
                $"{version.FolderName} · {version.Info.VersionType}, {version.Info.BuildType}, {number}";

            var score = Math.Max(
                name.StartsWith(query, Cmp) ? 130 : name.Contains(query, Cmp) ? 100 : 0,
                number.Contains(query, Cmp) ? 70 : 0);
            if (score <= 0) continue;

            var config = version;
            results.Add(new SearchResult
            {
                Title = name,
                Description = description,
                Category = i18n["Search.Category.Instance"],
                Glyph = "\uE7C3",
                Payload = config,
                Score = score,
                Execute = () => UiGlobal.MainWindow?.OpenDraw(new DrawInstanceContent(config),
                    $"{config.Info.VersionName} - {config.Info.Version}")
            });
        }

        // 聚合搜索兜底：任何关键词都可以丢给下载中心
        results.Add(new SearchResult
        {
            Title = string.Format(i18n["Search.Action.Aggregate.Title"], query),
            Description = i18n["Search.Action.Aggregate.Desc"],
            Category = i18n["Search.Category.Aggregate"],
            Glyph = "\uE721",
            Score = -1,
            Execute = () => Views.Pages.DownloadPage.DownloadSearch.SearchFromGlobal(query)
        });

        return results.OrderByDescending(x => x.Score).Take(15).ToList();
    }

    private static int MatchScore(string title, string keywords, string description, string query)
    {
        if (string.IsNullOrEmpty(title)) return 0;
        if (title.StartsWith(query, Cmp)) return 120;
        if (title.Contains(query, Cmp)) return 90;
        if (!string.IsNullOrEmpty(keywords) && keywords.Contains(query, Cmp)) return 60;
        if (!string.IsNullOrEmpty(description) && description.Contains(query, Cmp)) return 40;
        return 0;
    }

    private List<VersionConfig> GetInstances()
    {
        if (_instanceCache != null && (DateTime.UtcNow - _instanceCacheTime).TotalSeconds < 5)
            return _instanceCache;

        var list = new List<VersionConfig>();
        try
        {
            var folders = CoreGlobal.Config?.Data?.GameFolders;
            if (folders != null)
                foreach (var folder in folders)
                {
                    if (string.IsNullOrEmpty(folder?.GameFolderPath)) continue;

                    foreach (var version in GameInfoHelper.GetVersionConfigs(folder.GameFolderPath))
                    {
                        version.FolderName = string.IsNullOrEmpty(folder.GameFolderName)
                            ? System.IO.Path.GetFileName(folder.GameFolderPath)
                            : folder.GameFolderName;
                        list.Add(version);
                    }
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"搜索实例失败: {ex.Message}");
        }

        _instanceCache = list;
        _instanceCacheTime = DateTime.UtcNow;
        return list;
    }

    private List<SearchEntry> BuildEntries()
    {
        var list = new List<SearchEntry>();

        void Add(string title, string keywords, string glyph, string categoryKey, string? description,
            Action execute)
        {
            var capturedDescription = description;
            list.Add(new SearchEntry
            {
                Title = () => title,
                Description = capturedDescription == null ? null : () => capturedDescription,
                Keywords = keywords,
                Glyph = glyph,
                Category = () => i18n[categoryKey],
                Execute = execute
            });
        }

        // —— 标题使用 i18n 键时在查询时解析，切换语言后无需重建索引 ——
        void AddI18n(string titleKey, string keywords, string glyph, string categoryKey,
            Action execute, string? descriptionKey = null)
        {
            list.Add(new SearchEntry
            {
                Title = () => i18n[titleKey],
                Description = descriptionKey == null ? null : () => i18n[descriptionKey],
                Keywords = keywords,
                Glyph = glyph,
                Category = () => i18n[categoryKey],
                Execute = execute
            });
        }

        Action<Func<ISettingPage>> setting = factory =>
            OpenSetting(factory);

        void Setting(string titleKey, string keywords, string glyph, Func<ISettingPage> page,
            string? descriptionKey = null)
            => AddI18n(titleKey, keywords, glyph, "Search.Category.Setting", () => setting(page), descriptionKey);

        // —— 主页面 ——
        AddI18n("MainPage.Nav.Home", "主页 首页 home start", "\uE80F", "Search.Category.Page",
            () => MainPage.Instance?.SelectTagPage("main:home"));
        AddI18n("MainPage.Nav.Manager", "实例 管理 实例管理 manager instance", "\uE71D",
            "Search.Category.Page", () => MainPage.Instance?.SelectTagPage("main:manager"));
        AddI18n("MainPage.Nav.Download", "下载 download 资源", "\uE896", "Search.Category.Page",
            () => MainPage.Instance?.SelectTagPage("main:download"));
        if (OperatingSystem.IsWindows())
            AddI18n("MainPage.Nav.Tools", "工具 工具箱 tools box", "\uE90F", "Search.Category.Page",
                () => MainPage.Instance?.SelectTagPage("main:toolsBox"));
        AddI18n("MainPage.Nav.Multiplayer", "联机 多人 multiplayer gravitycone", "\uF0B9",
            "Search.Category.Page", () => MainPage.Instance?.SelectTagPage("main:multiplayer"));
        AddI18n("MainPage.Nav.Setting", "设置 setting options", "\uE713", "Search.Category.Page",
            () => MainPage.Instance?.SelectTagPage("main:setting"));

        // —— 设置：分区页 ——
        Setting("Settings.Nav.Game.Title", "游戏 game", "\uE7FC", () => new SettingGame(),
            "Settings.Nav.Game.Desc");
        Setting("Settings.Nav.Download.Title", "下载 download 源", "\uE896", () => new SettingDownload(),
            "Settings.Nav.Download.Desc");
        Setting("Settings.Nav.Personalization.Title", "个性化 主题 外观 personalization theme",
            "\uEE56", () => new SettingPersonalization(), "Settings.Nav.Personalization.Desc");
        Setting("Settings.Nav.Universal.Title", "通用 universal", "\uE71D", () => new SettingUniversal(),
            "Settings.Nav.Universal.Desc");
        Add("账户与档案", "账户 档案 账号 xbox 微软 microsoft account profile", "\uE716",
            "Search.Category.Setting", "XBOX 账户与设置", () => setting(() => new SettingAccount()));
        Add("辅助功能", "辅助 无障碍 accessibility", "\uE805", "Search.Category.Setting", null,
            () => setting(() => new SettingAccessibility()));
        Setting("Settings.Nav.Plugin.Title", "插件 扩展 plugin extension", "\uEA86",
            () => new SettingPlugin(), "Settings.Nav.Plugin.Desc");
        Setting("Settings.Nav.About.Title", "关于 about 版本 开源 贡献", "\uE946", () => new AboutPage(),
            "Settings.Nav.About.Desc");

        // —— 设置：游戏 ——
        Setting("Setting.Game.Folders.Title", "文件夹 目录 路径 实例 folder path directory", "\uE7FC",
            () => new GameFolders());
        Setting("Settings.Game.Backup.Title", "备份 存档 还原 backup", "\uE7FC", () => new GameBackup());
        Setting("Settings.Game.MouseLock.Title", "鼠标 锁定 mouse lock", "\uE7FC", () => new MouseLock());
        Add("Proton", "proton linux 兼容层 wine steam", "\uE7FC", "Search.Category.Setting", null,
            () => setting(() => new GameProton()));
        Setting("Settings.Game.PublicSync.Title", "同步 公共 sync public", "\uE7FC",
            () => new GamePublicSync());
        Setting("Settings.Game.LaunchCommand.Title", "命令 参数 启动 command launch", "\uE7FC",
            () => new GameLaunchCommand());

        // —— 设置：下载 ——
        Setting("Settings.Download.NetworkTest.Title", "网络 测速 延迟 network test", "\uE896",
            () => new NetworkTest());

        // —— 设置：个性化 ——
        Setting("Settings.Personalization.Color.Title", "颜色 主题色 强调 color accent", "\uEE56",
            () => new PersonalizationColor());
        Setting("Settings.Personalization.Background.Title", "背景 壁纸 background wallpaper", "\uEE56",
            () => new PersonalizationBackground());
        Setting("Settings.Personalization.Home.Title", "主页 布局 home layout", "\uEE56",
            () => new PersonalizationHome());
        Setting("Settings.Personalization.Audio.Title", "音频 声音 音量 audio sound volume", "\uEE56",
            () => new PersonalizationAudio());
        Setting("Settings.Personalization.ThemePackManager.Title", "主题包 theme pack", "\uEE56",
            () => new PersonalizationThemePack());
        Setting("Settings.Personalization.Font.Title", "字体 font", "\uEE56", () => new PersonalizationFont());

        // —— 设置：通用 ——
        Setting("Setting.Universal.SoftwareUpdate.Title", "软件更新 升级 update", "\uE71D",
            () => new UniversalSoftwareUpdate());
        Setting("Setting.Universal.Debug.Title", "调试 开发者 debug", "\uE71D", () => new UniversalDebug());
        Add("语言 / Language", "语言 language", "\uE71D", "Search.Category.Setting", null,
            () => setting(() => new UniversalLanguage()));
        Setting("Settings.Universal.Update.Dependency.Title", "依赖 运行库 dependency", "\uE71D",
            () => new UniversalDependencyUpdate());
        Setting("Setting.Universal.Exception.Title", "异常 崩溃 报错 exception crash", "\uE71D",
            () => new UniversalException());

        // —— 设置：账户 / 插件 ——
        Add("Xbox 账户", "xbox 账户 登录 微软 live", "\uE716", "Search.Category.Setting", null,
            () => setting(() => new AccountXbox()));
        Add("插件管理", "插件 管理 manager plugin", "\uEA86", "Search.Category.Setting", null,
            () => setting(() => new PluginManager()));

        return list;
    }

    /// <summary>
    ///     先切到设置页，再跳到目标设置页；设置框架在设置页构造时完成初始化。
    /// </summary>
    private static void OpenSetting(Func<ISettingPage> pageFactory)
    {
        var main = MainPage.Instance;
        if (main == null) return;

        main.SelectTagPage("main:setting");

        try
        {
            MainSettingPage.NavigateTo(pageFactory());
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"搜索跳转设置页失败: {ex.Message}");
        }
    }
}
