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

using System.Collections.Generic;
using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameFolders : ISettingPage
{
    public bool IsEdit;

    public GameFolders()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Folders.Title"]
            }
        };

        UpdateUI();
        IsEdit = true;
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.Title"],
            Content = dialog,
            CloseButtonText = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.Action"],
            SecondaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
            PrimaryButtonText = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.ImportOther"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();

                    UpdateUI();
                }
            },
            PrimaryAction = () =>
            {
                Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawImportOtherLauncherContent(),
                    I18nManager.Instance["Setting.Game.Folders.Draw.Import.Title"]);
            }
        });
    }

    private void UpdateUI()
    {
        ListBox.Children.Clear();

        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            ListBox.Children.Add(new GameFolderSettingItem(folder, UpdateUI));
        });
    }
}