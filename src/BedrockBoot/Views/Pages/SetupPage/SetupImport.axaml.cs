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

using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Core.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupImport : UserControl
{
    public SetupImport()
    {
        InitializeComponent();
    }

    private void ImportFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo
        {
            // 标题和按钮文本国际化
            Title = I18nManager.Instance["Setup.Import.Dialog.AddFolder.Title"],
            Content = dialog,
            CloseButtonText = I18nManager.Instance["Setup.Import.Dialog.AddFolder.Action"],
            SecondaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    // 逻辑保持不变：如果文件夹名为空，则取路径名
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();
                }
            }
        });
    }

    private void ImportOtherLauncherBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 侧边抽屉标题国际化
        Models.Global.GlobalModel.MainWindow.OpenDraw(
            new DrawImportOtherLauncherContent(),
            I18nManager.Instance["Setup.Import.Draw.ImportOther.Title"]
        );
    }
}