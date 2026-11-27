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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Chunker;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Chunker;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Window = Avalonia.Controls.Window;

namespace BedrockBoot.Views.DrawContent.Chunker;

public partial class DrawChunkerJavaToBedrockContent : UserControl
{
    public DrawChunkerJavaToBedrockContent()
    {
        InitializeComponent();
    }

    private async void ImportArchive_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = I18nManager.Instance["UI.Draw.SelectJavaWorldArchive"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(I18nManager.Instance["FileType.Zip"])
                {
                    Patterns = new[] { "*.zip" }
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

    private async void SaveMcWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["UI.Draw.SaveAsMcWorld"],
            DefaultExtension = "mcworld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(I18nManager.Instance["UI.Draw.FileTypeBedrockWorld"])
                {
                    Patterns = new[] { "*.mcworld" }
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
                    ChunkerType.JavaToBedrock,
                    SaveType.File,
                    BedrockBoot.Chunker.Chunker.SupportBedrock[GameVersionChoose.SelectedIndex],
                    WorldPath.Text!,
                    result)
            });
        }
    }

    private void SaveIns_OnClick(object? sender, RoutedEventArgs e)
    {
        var ins = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["UI.Draw.SelectInstance"],
            Content = ins,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            CloseAction = () =>
            {
                var insInfo = ins.VersionConfig;

                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["UI.Draw.ConvertSave"],
                    Content = new DialogChunkerConversionContent(
                        ChunkerType.JavaToBedrock,
                        SaveType.File,
                        BedrockBoot.Chunker.Chunker.SupportBedrock[GameVersionChoose.SelectedIndex],
                        WorldPath.Text!,
                        Path.Combine(ChunkerHelper.ChunkerTempFolderPath,
                            $"pack_input_{Guid.NewGuid().ToString().Replace("-", "")}.mcworld"),
                        s =>
                        {
                            Task.Run(() =>
                            {
                                var acrh = new ArchiveCheck(insInfo);
                                acrh.Check();
                                acrh.ImportWorldPack(s);

                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                                    {
                                        Title = I18nManager.Instance["Task.ImportPack.Status.Completed"],
                                        Message = I18nManager.Instance["UI.Draw.ConversionImportCompleted"]
                                    });
                                });
                            });
                        })
                });
            },
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton
        });
    }
}