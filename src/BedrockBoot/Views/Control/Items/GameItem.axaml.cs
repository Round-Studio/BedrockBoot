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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Localization;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class GameItem : UserControl
{
    private ImageLoader _imageLoader = new ImageLoader();

    public GameItem()
    {
        InitializeComponent();

        // 版本类型、游玩时长都是普通字符串属性（不是 {DynamicResource}），
        // 卡片创建后不会自己变，键表一变（切语言 / 调试预览换文件）就要重建一次。
        Loaded += (_, _) =>
        {
            LocalizationContext.Current.Changed -= OnTableChanged;
            LocalizationContext.Current.Changed += OnTableChanged;
        };
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        LocalizationContext.Current.Changed -= OnTableChanged;
        _imageLoader.Dispose();
    }

    private void OnTableChanged(object? sender, LocalizationChangedEventArgs e)
    {
        if (VersionInfo is null)
            return;

        if (Dispatcher.UIThread.CheckAccess())
            RefreshLabels();
        else
            Dispatcher.UIThread.Post(RefreshLabels);
    }

    /// <summary>
    ///     只重建文字标签，不重跑 <see cref="Update" />：那条路径会重新加载图标，
    ///     而图标加载器在卡片卸载时已经释放，切语言时代不一定还能用。
    /// </summary>
    private void RefreshLabels()
    {
        UpdateHeaderLabels();
        UpdateDescription();
    }

    public GameItem(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }

    public async Task Update()
    {
        VersionName.Text = VersionInfo.Info.VersionName;

        RefreshLabels();

        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        // 图标只显示 32x32，按 64 宽解码
        Card.ImageIcon = await _imageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(VersionInfo), 64);
        if (LeviLamina.IsInstallModLoader(VersionInfo.VersionPath!))
            ModLoadersPanel.Children.Add(new LabelBox()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = "LeviLamina"
            });
    }

    /// <summary>
    ///     实例名后面挂两枚标签：版本类型（正式版/测试版…）与构建类型（UWP/GDK）。
    ///     构建类型是固定缩写，不参与本地化。
    /// </summary>
    private void UpdateHeaderLabels()
    {
        HeaderLabelsPanel.Children.Clear();

        HeaderLabelsPanel.Children.Add(new LabelBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = GetVersionTypeText(VersionInfo.Info.VersionType),
             Background = GetResourceBrush("AccentSubtleBrush", new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)))

        });

        HeaderLabelsPanel.Children.Add(new LabelBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = VersionInfo.Info.BuildType.ToString(),
            // 弱化处理，避免和启动按钮抢主色
            Background = GetResourceBrush("AccentSubtleBrush", new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)))
        });

        HeaderLabelsPanel.IsVisible = true;
    }
    private IBrush GetResourceBrush(string resourceKey, IBrush fallback)
    {
        if (this.TryFindResource(resourceKey, out var res) && res is IBrush brush)
        {
            return brush;
        }
        return fallback;
    }
    private static string GetVersionTypeText(MinecraftGameTypeVersion versionType)
    {
        return versionType switch
        {
            MinecraftGameTypeVersion.Release => I18nManager.Instance["Manager.GameType.Release"],
            MinecraftGameTypeVersion.Preview => I18nManager.Instance["Manager.GameType.Preview"],
            // Beta 表示未识别/无效类型，直接显示枚举名
            _ => versionType.ToString()
        };
    }

    /// <summary>说明行：内部版本号 · 游玩时长。</summary>
    private void UpdateDescription()
    {
        var hours = TimeSpan.FromSeconds(VersionInfo.PlayerData?.TotalPlayTime ?? 0).TotalHours;
        var playTime = string.Format(I18nManager.Instance["Manager.GameItem.PlayTime"], hours.ToString("F2"));

        DescriptionText.Text = string.IsNullOrEmpty(VersionInfo.Info.Version)
            ? playTime
            : $"{VersionInfo.Info.Version} · {playTime}";
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskLaunchGameItem.Launch(VersionInfo);
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(VersionInfo),
            $"{VersionInfo.Info.VersionName} - {VersionInfo.Info.Version}", "\uE7FC");
    }
}