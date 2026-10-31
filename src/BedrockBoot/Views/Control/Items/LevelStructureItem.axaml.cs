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
            ? "世界数据库"
            : $@"structures/{_info.StructureId.Replace(':', '/')}.mcstructure";

        var detail = _info.IsValid
            ? $@"尺寸 {_info.SizeX}×{_info.SizeY}×{_info.SizeZ}（{_info.BlockCount:N0} 个方块，{_info.PaletteCount} 种方块），{FormatSize(_info.FileSize)}"
            : $@"无法解析此结构文件，{FormatSize(_info.FileSize)}";

        StructureInfo.Text = _info.Source == StructureSource.File
            ? $@"{detail}，{_info.ModifiedTime:yyyy/MM/dd HH:mm}"
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
            Title = "导出结构",
            SuggestedFileName = _info.StructureId.Replace(':', '_') + ".mcstructure",
            DefaultExtension = "mcstructure",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Minecraft 结构文件 (*.mcstructure)")
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
                          ?? throw new IOException("结构已不存在");
                File.WriteAllBytes(localPath, nbt);
            }
            else
            {
                File.Copy(_info.FilePath, localPath, true);
            }

            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = "导出结构",
                Message = $@"已成功导出 {_info.StructureId}",
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
            Title = "删除警告",
            Content = $@"你确定要删除结构 {_info.StructureId} 吗？该操作无法撤销。",
            CloseButtonText = "确定删除",
            PrimaryButtonText = "取消",
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
