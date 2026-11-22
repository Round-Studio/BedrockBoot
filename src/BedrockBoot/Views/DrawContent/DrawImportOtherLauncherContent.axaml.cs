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
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawImportOtherLauncherContent : UserControl
{
    public DrawImportOtherLauncherContent()
    {
        InitializeComponent();
        InitializeLauncherList();
    }

    private static I18nManager i18n => I18nManager.Instance;

    /// <summary>
    ///     初始化并渲染可导入的启动器列表
    /// </summary>
    private void InitializeLauncherList()
    {
        LaunchersBox.Children.Clear();
        var anyVisible = false;

        // PathsList.OtherLauncher 定义了支持扫描的启动器列表
        foreach (var launcher in GlobalModel.OtherLauncher)
            // 如果配置文件存在，或者该启动器标记为强制显示（!IsExists）
            if (File.Exists(launcher.ConfigFile) || !launcher.IsExists)
            {
                anyVisible = true;

                var item = new SettingCard
                {
                    IsClickable = true,
                    // 假设框架属性名为 IsFontIcon，设为 false 以显示图片
                    IsFontIcon = false,
                    ImageIcon = LoadResourceBitmap(launcher.IconUrl),
                    Header = launcher.Name,
                    Description = string.Format(i18n["Import.Launcher.Description.Format"], launcher.Name)
                };

                // 绑定点击事件执行具体的导入逻辑
                item.Click += (sender, args) =>
                {
                    launcher.OnImport?.Invoke(launcher.ConfigFile);
                    // 导入后通常关闭侧边栏
                    GlobalModel.MainWindow.CloseDraw();
                };

                LaunchersBox.Children.Add(item);
            }

        // 如果没有检测到任何启动器，显示“无内容”提示
        NoneBox.IsVisible = !anyVisible;
    }

    /// <summary>
    ///     安全加载资源图片
    /// </summary>
    private Bitmap? LoadResourceBitmap(string url)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(url));
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load icon {url}: {ex.Message}");
            return null;
        }
    }
}