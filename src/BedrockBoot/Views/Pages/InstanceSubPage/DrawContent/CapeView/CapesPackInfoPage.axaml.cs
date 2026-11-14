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
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesPackInfoPage : UserControl
{
    private readonly VersionConfig _versionInfo;
    private readonly string _folderName;
    private bool _isEdit;

    public CapesPackInfoPage()
    {
        InitializeComponent();
    }

    public CapesPackInfoPage(VersionConfig versionInfo, string folderName) : this()
    {
        _versionInfo = versionInfo;
        _folderName = folderName;
        UpdateContent();
    }

    private CapesPackInfo? LoadPack()
    {
        return CapesPackManager.LoadPacks(_versionInfo.VersionPath).Find(p => p.FolderName == _folderName);
    }

    private void UpdateContent()
    {
        _isEdit = false;

        var pack = LoadPack();
        PackNameBox.Text = pack?.PackName ?? string.Empty;
        AuthorBox.Text = pack?.Authors ?? string.Empty;

        _isEdit = true;
    }

    private void PackNameBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_isEdit) return;
        var name = PackNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        CapesPackManager.UpdatePackName(_versionInfo.VersionPath, _folderName, name);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (VisualRoot is CapesPackSetting host) host.UpdateUI();
        });
    }

    private void AuthorBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_isEdit) return;
        CapesPackManager.SetAuthors(_versionInfo.VersionPath, _folderName, AuthorBox.Text?.Trim() ?? string.Empty);
    }

    private async void PickIconBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择披风包图标",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
        });
        if (files is not { Count: >= 1 }) return;

        var bytes = await File.ReadAllBytesAsync(files[0].Path.LocalPath);
        CapesPackManager.SetIcon(_versionInfo.VersionPath, _folderName, bytes);
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "成功",
            Message = "披风包图标已更新",
            NoticeType = NoticeType.Info
        });
    }

    private async void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出披风包",
            SuggestedFileName = _folderName,
            DefaultExtension = "mccapes",
            FileTypeChoices = new[] { new FilePickerFileType("McCapes") { Patterns = new[] { "*.mccapes" } } }
        });
        if (file is null) return;

        var error = CapesPackManager.ExportPack(_versionInfo.VersionPath, _folderName, file.Path.LocalPath);
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = error is null ? "成功" : "导出失败",
            Message = error ?? string.Format("披风包已导出到 {0}", file.Path.LocalPath),
            NoticeType = error is null ? NoticeType.Info : NoticeType.Warning
        });
    }
}