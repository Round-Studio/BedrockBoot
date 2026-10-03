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
using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Entity.Config;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Style.Widgets;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using BedrockBoot.Views.DrawContent;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public DesktopWorkspace DesktopWorkspace;

    public MainHomePage()
    {
        InitializeComponent();
        this.Loaded += (s, e) => UpdateHome();
        Loaded += (_, _) =>
        {
            if (Models.Global.GlobalModel.CustomManifest?.PageTitles != null)
            {
                if (!string.IsNullOrEmpty(LineTextBlock.Text =
                        Models.Global.GlobalModel.CustomManifest.PageTitles.PageHome))
                    LineTextBlock.Text = Models.Global.GlobalModel.CustomManifest.PageTitles.PageHome.Replace(
                        "{{random}}",
                        Models.Global.GlobalModel.CustomManifest.RandomStr[
                            (new Random()).Next(0, Models.Global.GlobalModel.CustomManifest.RandomStr.Count)]);
            }
        };
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
            case HomeType.Widgets:
                DesktopWorkspace = new DesktopWorkspace();
                if (File.Exists(PathsList.WidgetsConfigPath))
                    DesktopWorkspace.ImportLayout(File.ReadAllText(PathsList.WidgetsConfigPath));
                DesktopWorkspace.AddWidgetCallOn += (sender, args) =>
                {
                    Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawAddWidgetContent(), "添加小组件");
                };
                DesktopWorkspace.LayoutChanged += (sender, args) =>
                {
                    var json = DesktopWorkspace.ExportLayout();
                    File.WriteAllText(PathsList.WidgetsConfigPath, json);
                };
                MainGrid.Children.Add(DesktopWorkspace);
                break;
        }
    }
}