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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class LevelStructureItem : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    private readonly McStructureInfo _info;
    private readonly ArchiveInfo _archiveInfo;

    public LevelStructureItem()
    {
        InitializeComponent();
    }

    public LevelStructureItem(ArchiveInfo archiveInfo, McStructureInfo info) : this()
    {
        _archiveInfo = archiveInfo;
        _info = info;
        UpdateUI();
    }

    public Action? RefreshCallBack { get; set; }

    private void UpdateUI()
    {
        StructureName.Text = _info.StructureId;
        StructurePath.Text = _info.Source == StructureSource.Database
            ? I18nManager.Instance["UI.Item.Structure.WorldDatabase"]
            : $@"structures/{_info.StructureId.Replace(':', '/')}.mcstructure";

        var detail = _info.IsValid
            ? string.Format(I18nManager.Instance["UI.Item.Structure.Detail"], _info.SizeX, _info.SizeY, _info.SizeZ, _info.BlockCount.ToString("N0"), _info.PaletteCount, FormatSize(_info.FileSize))
            : string.Format(I18nManager.Instance["UI.Item.Structure.InvalidDetail"], FormatSize(_info.FileSize));

        StructureInfo.Text = _info.Source == StructureSource.File
            ? string.Format(I18nManager.Instance["UI.Item.Structure.WithModified"], detail, _info.ModifiedTime.ToString("yyyy/MM/dd HH:mm"))
            : detail;
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $@"{size:0.#} {units[unit]}";
    }

    private async void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["LevelSettings.Structure.Export"],
            SuggestedFileName = _info.StructureId.Replace(':', '_') + ".mcstructure",
            DefaultExtension = "mcstructure",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(I18nManager.Instance["UI.Item.Structure.FileType"])
                {
                    Patterns = new[] { "*.mcstructure" }
                }
            }
        });

        var localPath = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(localPath)) return;

        try
        {
            if (_info.Source == StructureSource.Database)
            {
                var nbt = LevelStructureStore.ReadDatabaseStructure(_archiveInfo, _info.DbKey)
                          ?? throw new IOException(I18nManager.Instance["LevelSettings.Structure.Gone"]);
                File.WriteAllBytes(localPath, nbt);
            }
            else
            {
                File.Copy(_info.FilePath, localPath, true);
            }

            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["LevelSettings.Structure.Export"],
                Message = string.Format(I18nManager.Instance["LevelSettings.Structure.Exported"], _info.StructureId),
                NoticeType = NoticeType.Info
            });
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Message = ex.Message,
                NoticeType = NoticeType.Error
            });
        }
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Archive.Delete.Warn.Title"],
            Content = string.Format(I18nManager.Instance["LevelSettings.Structure.DeleteConfirm"], _info.StructureId),
            CloseButtonText = I18nManager.Instance["Archive.Delete.Warn.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            CloseAction = () =>
            {
                try
                {
                    if (_info.Source == StructureSource.Database)
                        LevelStructureStore.DeleteFromDatabase(_archiveInfo, _info.DbKey);
                    else
                        File.Delete(_info.FilePath);
                }
                catch (Exception ex)
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = ex.Message,
                        NoticeType = NoticeType.Error
                    });
                    return;
                }
                RefreshCallBack?.Invoke();
            }
        });
    }
}
