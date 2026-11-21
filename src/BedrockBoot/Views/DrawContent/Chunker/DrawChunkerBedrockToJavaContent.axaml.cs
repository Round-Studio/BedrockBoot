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
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Chunker;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent.Chunker;

public partial class DrawChunkerBedrockToJavaContent : UserControl
{
    public DrawChunkerBedrockToJavaContent()
    {
        InitializeComponent();
    }

    private async void ImportMcWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = I18nManager.Instance["UI.Draw.SelectMinecraftWorldFile"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(I18nManager.Instance["UI.Draw.FileTypeMinecraftWorld"])
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });

        if (files.Count > 0)
        {
            var selectedFile = files[0].Path.LocalPath;

            var extension = Path.GetExtension(selectedFile).ToLower();
            if (extension.Contains("zip"))
                WorldPath.Text = selectedFile;
            else
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["UI.Draw.WrongFileTitle"],
                    Content = I18nManager.Instance["UI.Draw.WrongFileZipContent"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
                });
        }
    }

    private void ChooseWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var insChoose = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["UI.Draw.SelectInstance"],
            Content = insChoose,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var worldsChoose = new DialogChooseGameWorldsContent(insChoose.VersionConfig);
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["UI.Draw.SelectSave"],
                    Content = worldsChoose,
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
                    PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = () =>
                    {
                        var arch = worldsChoose.SelectedArchiveInfo;
                        WorldPath.Text = arch!.Path;
                    }
                });
            }
        });
    }

    private async void SaveFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = I18nManager.Instance["UI.Draw.SelectExportFolder"]
        });

        if (folders.Count > 0)
        {
            var result = folders[0].Path.LocalPath;
            DialogHost.Show(new DialogInfo
            {
                Title = I18nManager.Instance["UI.Draw.ConvertSave"],
                Content = new DialogChunkerConversionContent(
                    ChunkerType.BedrockToJava,
                    SaveType.Folder,
                    BedrockBoot.Chunker.Chunker.SupportJava[GameVersionChoose.SelectedIndex],
                    WorldPath.Text!,
                    result)
            });
        }
    }

    private async void SaveZipFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["UI.Draw.SaveAsZip"],
            DefaultExtension = "zip",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(I18nManager.Instance["FileType.Zip"])
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (file != null)
        {
            var result = file.Path.LocalPath;
            DialogHost.Show(new DialogInfo
            {
                Title = I18nManager.Instance["UI.Draw.ConvertSave"],
                Content = new DialogChunkerConversionContent(
                    ChunkerType.BedrockToJava,
                    SaveType.File,
                    BedrockBoot.Chunker.Chunker.SupportJava[GameVersionChoose.SelectedIndex],
                    WorldPath.Text!,
                    result)
            });
        }
    }
}