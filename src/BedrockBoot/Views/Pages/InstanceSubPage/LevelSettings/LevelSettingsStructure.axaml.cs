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
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsStructure : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    private readonly ArchiveInfo _info;
    private List<McStructureInfo> _allStructures = new();
    private CancellationTokenSource? _loadCts;
    private bool _isLoading;

    public LevelSettingsStructure()
    {
        InitializeComponent();
    }

    public LevelSettingsStructure(ArchiveInfo info) : this()
    {
        _info = info;
        _loadCts = new CancellationTokenSource();
        DetachedFromLogicalTree += (_, _) => _loadCts.Cancel();
        RefreshStructures();
    }

    // 结构扫描可能遍历上百 MB 的世界数据库，必须放后台线程避免卡死 UI
    private async void RefreshStructures()
    {
        if (_isLoading) return;
        _isLoading = true;

        LoadingCard.IsVisible = true;
        NullBox.IsVisible = false;
        StructureScrollViewer.IsVisible = false;
        StructuresBox.Children.Clear();

        List<McStructureInfo> loaded;
        try
        {
            loaded = await Task.Run(() => LevelStructureStore.GetAllStructures(_info), _loadCts!.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _isLoading = false;
            LoadingCard.IsVisible = false;
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Message = ex.Message,
                NoticeType = NoticeType.Error
            });
            return;
        }

        if (_loadCts.IsCancellationRequested) return;

        _isLoading = false;
        LoadingCard.IsVisible = false;
        _allStructures = loaded;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var searchKey = SearchBox.Text?.Trim() ?? string.Empty;
        var structures = string.IsNullOrEmpty(searchKey)
            ? _allStructures
            : _allStructures.Where(x => x.StructureId.Contains(searchKey, StringComparison.OrdinalIgnoreCase)).ToList();

        StructuresBox.Children.Clear();
        NullBox.IsVisible = structures.Count == 0;
        StructureScrollViewer.IsVisible = structures.Count > 0;

        foreach (var structure in structures)
        {
            StructuresBox.Children.Add(new LevelStructureItem(_info, structure)
            {
                RefreshCallBack = RefreshStructures
            });
        }
    }

    private async void ImportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = i18n["LevelSettings.Structure.Import.Picker.Title"],
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(i18n["LevelSettings.Structure.Import.Picker.Type"])
                {
                    Patterns = new[] { "*.mcstructure" }
                }
            }
        });

        if (files.Count == 0) return;

        var targets = new List<(string StructureId, byte[] Nbt)>();
        foreach (var file in files)
        {
            var localPath = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath)) continue;
            try
            {
                var structureId = LevelStructureStore.NormalizeStructureId(Path.GetFileNameWithoutExtension(localPath));
                targets.Add((structureId, File.ReadAllBytes(localPath)));
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

        if (targets.Count == 0) return;

        var conflicts = targets
            .Select(x => x.StructureId)
            .Where(id => _allStructures.Any(s => s.StructureId.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (conflicts.Count > 0)
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "覆盖确认",
                Content = "以下结构已存在，继续导入将会覆盖：\n" + string.Join("\n", conflicts),
                CloseButtonText = "覆盖导入",
                PrimaryButtonText = "取消",
                CloseAction = () => ImportTargets(targets)
            });
        }
        else
        {
            ImportTargets(targets);
        }
    }

    private void ImportTargets(List<(string StructureId, byte[] Nbt)> targets)
    {
        try
        {
            LevelStructureStore.WriteToDatabase(_info, targets);
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["LevelSettings.Structure.Action.Import"],
                Message = string.Format(i18n["LevelSettings.Structure.Import.Success"], targets.Count),
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
            return;
        }
        RefreshStructures();
    }

    private void RefreshBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshStructures();
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchBox != null && !_isLoading) ApplyFilter();
    }
}
