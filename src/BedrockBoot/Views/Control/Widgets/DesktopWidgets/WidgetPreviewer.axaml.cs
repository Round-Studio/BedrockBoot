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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetPreviewer : UserControl
{
    private readonly WidgetRegisterInfo _wid;

    public WidgetPreviewer()
    {
        InitializeComponent();
    }

    public WidgetPreviewer(WidgetRegisterInfo wid) : this()
    {
        _wid = wid;
        var content = DesktopWorkspace.CreateWidgetFromType(wid.Type);
        WidgetName.Text = wid.Name;
        WidgetDescription.Text = wid.Description;
        ContentControl.Content = content;
    }

    private void AddBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // "游戏版本"组件是要固定某个实例的：先选版本，选完再落地，
        // 否则加进来只会得到一个跟着主页当前选中走的空壳。
        if (_wid.Type == WidgetType.LeastPlay)
        {
            OpenVersionPickerThenAdd();
            return;
        }

        GlobalModel.MainWindow.CloseDraw();
        DesktopWorkspace.Instance?.AddWidget(_wid.Type);
    }

    private void OpenVersionPickerThenAdd()
    {
        // 在同一个抽屉里换页：先关再开会有两次滑入动画，视觉上很跳
        GlobalModel.MainWindow.SwitchDraw(
            new DrawSelectGameVersionContent((gameFolderPath, versionFolder) =>
            {
                DesktopWorkspace.Instance?.AddWidget(WidgetType.LeastPlay, layout =>
                {
                    layout.GameFolderPath = gameFolderPath;
                    layout.GameVersionFolder = versionFolder;
                });
            }),
            I18nManager.Instance["Widget.LaunchGame.ChooseVersion"]);
    }
}
