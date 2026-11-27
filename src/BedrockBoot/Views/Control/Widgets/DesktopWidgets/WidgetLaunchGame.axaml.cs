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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using BedrockBoot.Standard.Entity.Config;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models;
using BedrockBoot.Models.Helper;
using BedrockBoot.Style.Widgets;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetLaunchGame : IWidgetTemplated, IWidgetConfigurable
{
    /// <summary>实例名下划线静止时的高度。</summary>
    private const double NameLineRestHeight = 8;

    /// <summary>实例名光晕条静止时的高度。</summary>
    private const double NameHaloRestHeight = 6;

    private bool _isUpdating;
    private bool _reloadRequested;
    private bool _isNameHovered;
    private WidgetLayoutData? _layoutConfig;
    private ImageLoader? _imageLoader;

    /// <summary>组件固定的游戏目录；为空表示跟随实例列表中当前选中的目录。</summary>
    /// <remarks>
    ///     绑定值单独存一份，不依赖 <see cref="_layoutConfig" /> 是否被换掉：
    ///     布局数据在多处被读写，任何一次替换都不该让用户选好的版本悄悄回退。
    /// </remarks>
    private string? _pinnedFolderPath;

    /// <summary>组件固定的实例目录名；为空表示跟随实例列表中当前选中的版本。</summary>
    private string? _pinnedVersionFolder;

    /// <summary>
    ///     图标加载器按需创建：组件拖拽时会经历一次卸载/重新挂载，
    ///     卸载时释放过的实例不能再复用，否则后续刷新会抛 ObjectDisposedException。
    /// </summary>
    private ImageLoader Loader => _imageLoader ??= new ImageLoader();

    public WidgetLaunchGame()
    {
        SupportWidgetSize = new()
        {
            WidgetSize.ExtraLarge,
            WidgetSize.Large
        };
        InitializeComponent();

        _ = UpdateUIAsync();

        GlobalModel.Config.AfterSave += async (sender, args) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await UpdateUIAsync();
            });
        };

        LaunchButton.Click += LaunchButton_OnClick;
        NameHoverHost.PointerPressed += OnNamePointerPressed;
        NameHoverHost.PointerEntered += OnNamePointerEntered;
        NameHoverHost.PointerExited += OnNamePointerExited;
    }

    #region 组件配置

    public bool HasSettings => true;

    public string SettingsText => I18nManager.Instance["Widget.LaunchGame.ChooseVersion"];

    /// <summary>组件当前绑定的游戏目录；为空表示跟随实例列表中当前选中的目录。</summary>
    public string? PinnedGameFolderPath => _pinnedFolderPath;

    /// <summary>组件当前绑定的实例目录名；为空表示跟随实例列表中当前选中的版本。</summary>
    public string? PinnedVersionFolder => _pinnedVersionFolder;

    public void ShowSettings()
    {
        BedrockBoot.Models.Global.GlobalModel.MainWindow.OpenDraw(
            new DrawSelectGameVersionContent(this),
            I18nManager.Instance["Widget.LaunchGame.ChooseVersion"]);
    }

    public override void LoadWidgetConfig(WidgetLayoutData config)
    {
        _layoutConfig = config;
        _pinnedFolderPath = string.IsNullOrEmpty(config.GameFolderPath) ? null : config.GameFolderPath;
        _pinnedVersionFolder = string.IsNullOrEmpty(config.GameVersionFolder) ? null : config.GameVersionFolder;
        _ = UpdateUIAsync();
    }

    /// <summary>
    ///     把组件固定到某个实例；任一项传空表示恢复"跟随当前选中的实例"。
    /// </summary>
    public void ApplyGameSelection(string? gameFolderPath, string? versionFolder)
    {
        _pinnedFolderPath = string.IsNullOrEmpty(gameFolderPath) ? null : gameFolderPath;
        _pinnedVersionFolder = string.IsNullOrEmpty(versionFolder) ? null : versionFolder;

        // 同步回布局数据（导出布局时读的就是它），然后立刻写盘：
        // 只靠事件链保存的话，选中后马上离开主页就可能丢掉这次修改。
        if (_layoutConfig != null)
        {
            _layoutConfig.GameFolderPath = _pinnedFolderPath;
            _layoutConfig.GameVersionFolder = _pinnedVersionFolder;
        }

        // 组件已经不在可视树上时（面板开着的时候主页被重建过）不要写盘：
        // 那会把当前真实的组件集合导出，反而抹掉这次选择。
        if (TopLevel.GetTopLevel(this) != null)
            DesktopWorkspace.Instance?.SaveLayoutNow();

        _ = UpdateUIAsync();
    }

    /// <summary>取实例的目录名，作为跨会话仍稳定的绑定标识。</summary>
    public static string GetVersionFolderName(VersionConfig version)
    {
        if (!string.IsNullOrEmpty(version.VersionPath))
            return Path.GetFileName(version.VersionPath.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        return version.FolderName ?? string.Empty;
    }

    #endregion

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // 重新挂载后悬停状态已经作废（拖动/切页可能就是在悬停时发生的），先复位再刷新
        SetNameHovered(false);

        // 组件会被反复卸载/挂载（拖动、切换页面都会经历），
        // 每次挂回来都按当前绑定重算一次，避免界面上停在上一次的结果。
        _ = UpdateUIAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _imageLoader?.Dispose();
        _imageLoader = null;
    }

    /// <summary>点实例名直接打开版本选择面板（悬停样式已经暗示这里可点）。</summary>
    private void OnNamePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        // 标记已处理，避免同时被当成"拖动组件"的起点
        e.Handled = true;
        ShowSettings();
    }

    private void OnNamePointerEntered(object? sender, PointerEventArgs e) => SetNameHovered(true);

    private void OnNamePointerExited(object? sender, PointerEventArgs e) => SetNameHovered(false);

    /// <summary>
    ///     复刻主页欢迎语的悬停效果：下划线/光晕从底部那条细线长满整块，文字转白。
    /// </summary>
    /// <remarks>
    ///     这里用代码驱动高度而不是在样式里写 `:pointerover Border#... { Height = 绑定 }`：
    ///     祖先伪类 + RelativeSource 绑定的写法在运行时不让这两个 Border 长高（只有文字变白）。
    ///     文字也不能靠继承改色——OnePointUI 全局 `TextBlock` 样式优先级高于继承值，
    ///     所以逐个字符控件写本地值，才能盖住那条全局样式。
    /// </remarks>
    private void SetNameHovered(bool hovered)
    {
        _isNameHovered = hovered;

        var fullHeight = Math.Max(NameHoverHost.Bounds.Height, NameLineRestHeight);
        PATH_NameLineBorder.Height = hovered ? fullHeight : NameLineRestHeight;
        PATH_NameHaloBorder.Height = hovered ? fullHeight : NameHaloRestHeight;

        foreach (var textBlock in GameNameText.GetVisualDescendants().OfType<TextBlock>())
        {
            if (hovered)
                textBlock.Foreground = Brushes.White;
            else
                textBlock.ClearValue(TextBlock.ForegroundProperty);
        }
    }

    private Bitmap GetImage(string url)
    {
        var uri = new Uri(url);
        using (var stream = AssetLoader.Open(uri))
        {
            return new Bitmap(stream);
        }
    }

    private async Task UpdateUIAsync()
    {
        if (_isUpdating)
        {
            // 正在刷新时又被要求刷新（例如刚切换了绑定实例），当前这轮结束后再补一次
            _reloadRequested = true;
            return;
        }

        _isUpdating = true;

        try
        {
            var (version, pinnedMissing) = await ResolveTargetAsync();

            if (version != null)
                await UpdateGameInfo(version);
            else
                SetEmptyState(pinnedMissing
                    ? I18nManager.Instance["Widget.LaunchGame.VersionMissing"]
                    : I18nManager.Instance["Widget.LaunchGame.NoVersion"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[WidgetLaunchGame] 更新 UI 失败：{ex.Message}");
            SetEmptyState(I18nManager.Instance["Widget.LaunchGame.NoVersion"]);
        }
        finally
        {
            _isUpdating = false;
        }

        // 文字换了，逐字符控件是新建的：如果鼠标正悬在实例名上，把白色重新刷上去
        if (_isNameHovered) SetNameHovered(true);

        if (_reloadRequested)
        {
            _reloadRequested = false;
            await UpdateUIAsync();
        }
    }

    /// <summary>
    ///     解析要展示/启动的实例：
    ///     组件绑定过实例就用绑定的那个，否则沿用实例列表中当前选中的版本。
    /// </summary>
    private async Task<(VersionConfig? Version, bool PinnedMissing)> ResolveTargetAsync()
    {
        var config = GlobalModel.Config.Data;
        var folders = config.GameFolders;

        if (folders == null || folders.Count == 0) return (null, false);

        var pinnedFolderPath = _pinnedFolderPath;
        if (!string.IsNullOrEmpty(pinnedFolderPath))
        {
            var pinnedFolder = folders.FirstOrDefault(f =>
                string.Equals(f.GameFolderPath, pinnedFolderPath, StringComparison.OrdinalIgnoreCase));

            if (pinnedFolder == null) return (null, true);

            var pinnedVersions = await GameInfoHelper.GetVersionConfigsAsync(pinnedFolder.GameFolderPath);
            if (pinnedVersions == null || pinnedVersions.Count == 0) return (null, true);

            var pinnedVersion = FindVersion(pinnedVersions, _pinnedVersionFolder);
            return (pinnedVersion, pinnedVersion == null);
        }

        if (config.GameFolderSelIndex < 0 || config.GameFolderSelIndex >= folders.Count)
            return (null, false);

        var gameFolder = folders[config.GameFolderSelIndex];
        var versions = await GameInfoHelper.GetVersionConfigsAsync(gameFolder.GameFolderPath);

        if (versions == null || versions.Count == 0) return (null, false);

        if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count)
        {
            gameFolder.GameSelIndex = 0;
            GlobalModel.Config.Save();
        }

        return (versions[gameFolder.GameSelIndex], false);
    }

    /// <summary>
    ///     按目录名找实例；目录名对不上时退化为按版本号/实例名匹配，尽量容忍用户重命名目录。
    /// </summary>
    private static VersionConfig? FindVersion(List<VersionConfig> versions, string? versionFolder)
    {
        if (string.IsNullOrEmpty(versionFolder)) return versions.FirstOrDefault();

        return versions.FirstOrDefault(v =>
                   string.Equals(GetVersionFolderName(v), versionFolder, StringComparison.OrdinalIgnoreCase))
               ?? versions.FirstOrDefault(v =>
                   !string.IsNullOrEmpty(v.Info?.Version) &&
                   string.Equals(v.Info.Version, versionFolder, StringComparison.OrdinalIgnoreCase))
               ?? versions.FirstOrDefault(v =>
                   !string.IsNullOrEmpty(v.Info?.VersionName) &&
                   string.Equals(v.Info.VersionName, versionFolder, StringComparison.OrdinalIgnoreCase));
    }

    private async Task UpdateGameInfo(VersionConfig version)
    {
        if (version == null)
        {
            SetEmptyState(I18nManager.Instance["Widget.LaunchGame.NoVersion"]);
            return;
        }

        GameNameText.Text = version.Info.VersionName;
        GameVersionText.Text = version.Info.Version;
        // 图标显示 48x48，按 96 宽解码
        GameIcon.Source = await Loader.LoadIconAsync(IconHelper.GetGameIconUrl(version), 96);
        LaunchButton.IsEnabled = true;
    }

    private void SetEmptyState(string versionText)
    {
        GameNameText.Text = I18nManager.Instance["Widget.LaunchGame.Title"];
        GameVersionText.Text = versionText;
        GameIcon.Source = GetImage("avares://BedrockBoot/Assets/Icon/Logo/Grass.png");
        LaunchButton.IsEnabled = false;
    }

    private async void LaunchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // 启动前重新解析一次，保证启动的就是组件当前显示的那个实例
            var (version, _) = await ResolveTargetAsync();
            if (version == null) return;

            TaskLaunchGameItem.Launch(version);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[WidgetLaunchGame] 启动游戏失败：{ex.Message}");
        }
    }
}
