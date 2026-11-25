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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

/// <summary>
///     挑选一个游戏实例：既能把已有组件固定到某个实例，也能在添加组件前先选好版本。
/// </summary>
public partial class DrawSelectGameVersionContent : UserControl
{
    private const string DefaultGameIconUrl = "avares://BedrockBoot/Assets/Icon/Logo/Grass.png";

    private static I18nManager I18n => I18nManager.Instance;

    private static Bitmap? _defaultIcon;

    private readonly WidgetLaunchGame? _widget;

    /// <summary>"添加组件"路径下的回调：选完由调用方决定怎么落地。</summary>
    private readonly Action<string?, string?>? _onSelected;

    private bool _loaded;

    public DrawSelectGameVersionContent()
    {
        InitializeComponent();
    }

    /// <summary>把已存在的组件固定到某个实例。</summary>
    public DrawSelectGameVersionContent(WidgetLaunchGame widget) : this()
    {
        _widget = widget;
        Loaded += OnLoaded;
    }

    /// <summary>添加组件前先选版本，选完回调出去（参数为游戏目录与实例目录名，空表示跟随当前）。</summary>
    public DrawSelectGameVersionContent(Action<string?, string?> onSelected) : this()
    {
        _onSelected = onSelected;
        // 这是"添加前先选"的路径，提示语要说清选完就直接加进来
        HintText.Text = I18n["Widget.LaunchGame.Drawer.Hint.Add"];
        Loaded += OnLoaded;
    }

    private static Bitmap? DefaultIcon => _defaultIcon ??= LoadDefaultIcon();

    /// <summary>选中的结果交给组件自己或调用方，然后收起面板。</summary>
    private void ApplySelection(string? gameFolderPath, string? versionFolder)
    {
        if (_widget != null)
            _widget.ApplyGameSelection(gameFolderPath, versionFolder);
        else
            _onSelected?.Invoke(gameFolderPath, versionFolder);

        GlobalModel.MainWindow.CloseDraw();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        await BuildListAsync();
    }

    private async Task BuildListAsync()
    {
        try
        {
            var folders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;

            if (folders == null || folders.Count == 0)
            {
                ShowEmpty();
                return;
            }

            var versionsByFolder = await Task.WhenAll(
                folders.Select(folder => GameInfoHelper.GetVersionConfigsAsync(folder.GameFolderPath)));

            VersionList.Children.Add(CreateFollowCurrentCard());

            var hasAnyVersion = false;

            for (var i = 0; i < folders.Count; i++)
            {
                var versions = versionsByFolder[i];
                if (versions == null || versions.Count == 0) continue;

                hasAnyVersion = true;
                VersionList.Children.Add(CreateFolderHeader(folders[i]));

                foreach (var version in versions)
                    VersionList.Children.Add(CreateVersionCard(folders[i], version));
            }

            LoadRing.IsVisible = false;

            if (!hasAnyVersion)
            {
                // 一个实例都没有时，"跟随当前实例"也没有意义
                VersionList.Children.Clear();
                ShowEmpty();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[DrawSelectGameVersion] 加载实例列表失败：{ex.Message}");
            ShowEmpty();
        }
    }

    private void ShowEmpty()
    {
        LoadRing.IsVisible = false;
        EmptyBox.IsVisible = true;
    }

    private static TextBlock CreateFolderHeader(GameFolderInfo folder)
    {
        var name = string.IsNullOrWhiteSpace(folder.GameFolderName)
            ? folder.GameFolderPath
            : folder.GameFolderName;

        return new TextBlock
        {
            Text = name,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.7,
            Margin = new Thickness(4, 10, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private SettingCard CreateFollowCurrentCard()
    {
        var isFollowing = string.IsNullOrEmpty(_widget?.PinnedGameFolderPath);

        var card = new SettingCard
        {
            IsClickable = true,
            IsFontIcon = false,
            ImageIcon = DefaultIcon,
            Header = I18n["Widget.LaunchGame.FollowCurrent"],
            Description = I18n["Widget.LaunchGame.FollowCurrent.Desc"],
            Content = isFollowing ? CreateSelectedBadge() : null
        };

        card.Click += (_, _) => ApplySelection(null, null);

        return card;
    }

    private SettingCard CreateVersionCard(GameFolderInfo folder, VersionConfig version)
    {
        var folderName = WidgetLaunchGame.GetVersionFolderName(version);

        var isPinned = !string.IsNullOrEmpty(_widget?.PinnedGameFolderPath)
                       && string.Equals(_widget?.PinnedGameFolderPath, folder.GameFolderPath,
                           StringComparison.OrdinalIgnoreCase)
                       && string.Equals(_widget?.PinnedVersionFolder ?? string.Empty, folderName,
                           StringComparison.OrdinalIgnoreCase);

        var description =
            $"{version.Info.BuildType} · {version.Info.VersionType} · {version.Info.Version}";

        // 目录名和实例名不一致时补上目录名，避免同名实例分不清
        if (!string.Equals(folderName, version.Info.VersionName, StringComparison.OrdinalIgnoreCase))
            description = $"{description} · {folderName}";

        var card = new SettingCard
        {
            IsClickable = true,
            IsFontIcon = false,
            ImageIcon = DefaultIcon,
            Header = version.Info.VersionName,
            Description = description,
            Content = isPinned ? CreateSelectedBadge() : null
        };

        card.Click += (_, _) => ApplySelection(folder.GameFolderPath, folderName);

        _ = LoadIconAsync(card, version);

        return card;
    }

    private Avalonia.Controls.Control CreateSelectedBadge()
    {
        var badge = new TextBlock
        {
            Text = "\u2713",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (this.TryFindResource("AccentBackgroundBrush", out var brush) && brush is IBrush accent)
            badge.Foreground = accent;

        return badge;
    }

    private static async Task LoadIconAsync(SettingCard card, VersionConfig version)
    {
        try
        {
            var icon = await ImageLoader.Shared.LoadIconAsync(IconHelper.GetGameIconUrl(version), 64);
            if (icon != null) card.ImageIcon = icon;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[DrawSelectGameVersion] 加载实例图标失败：{ex.Message}");
        }
    }

    private static Bitmap? LoadDefaultIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(DefaultGameIconUrl));
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[DrawSelectGameVersion] 加载默认图标失败：{ex.Message}");
            return null;
        }
    }
}
