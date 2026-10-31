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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ContentView;

public partial class SavesView : UserControl
{
    public SavesView()
    {
        IsEdit = false;
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, SavesView_OnDragOver);
        AddHandler(DragDrop.DropEvent, SavesView_OnDrop);
    }

    public SavesView(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public VersionConfig VersionInfo { get; set; }
    public bool IsEdit { get; set; }
    public Action<ArchiveInfo>? EditAction { get; set; }

    private string SearchKey => SearchBox.Text ?? string.Empty;
    private string CurrentUser => (UserChooseBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

    private void UpdateUI()
    {
        IsEdit = false;

        var users = new ArchiveCheck(VersionInfo).GetInstanceWorldPackPath();

        UserChooseBox.Items.Clear();
        foreach (var user in users.Keys)
            UserChooseBox.Items.Add(new ComboBoxItem
            {
                Content = user
            });

        if (users.Count > 0)
        {
            UserChooseBox.SelectedIndex = 0;
            UpdateContent();
        }
        else
        {
            UpdateSaves(new List<ArchiveInfo>());
        }

        IsEdit = true;
    }

    public void UpdateSaves(List<ArchiveInfo> saves)
    {
        SavesBox.Children.Clear();
        NullBox.IsVisible = saves.Count <= 0;

        foreach (var save in saves)
            SavesBox.Children.Add(new ArchiveItem(save)
            {
                EditAction = () =>
                    EditAction?.Invoke(save),
                RefreshCallBack = UpdateUI
            });
    }

    public void UpdateTemplates(List<ResourcePackManifest> resPacks)
    {
        SavesBox.Children.Clear();
        NullBox.IsVisible = resPacks.Count <= 0;

        foreach (var save in resPacks)
            SavesBox.Children.Add(new GameResourcePackItem(save)
            {
                RefreshCallBack = UpdateUI
            });
    }

    private void UpdateContent()
    {
        if (TypeSel.SelectedIndex == 0)
        {
            if (string.IsNullOrEmpty(CurrentUser))
            {
                UpdateSaves(new List<ArchiveInfo>());
                return;
            }

            var currentSaves = new ArchiveCheck(VersionInfo).CheckUser(CurrentUser);

            if (!string.IsNullOrEmpty(SearchKey))
                currentSaves = currentSaves
                    .Where(s => s.Name.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            UpdateSaves(currentSaves);
        }
        else if (TypeSel.SelectedIndex == 1)
        {
            if (string.IsNullOrEmpty(CurrentUser))
            {
                UpdateTemplates(new List<ResourcePackManifest>());
                return;
            }

            var resManager = new ResourcePackManager(VersionInfo);
            var templates = resManager.GetAllPack(CurrentUser)
                .Where(x => x.PackType == ResourcePackType.WorldTemplate)
                .ToList();

            if (!string.IsNullOrEmpty(SearchKey))
            {
                var filtered = templates
                    .Where(t => t.Header.Name.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                UpdateTemplates(filtered);
            }
            else
            {
                UpdateTemplates(templates);
            }
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit) UpdateContent();
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit) UpdateContent();
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = i18n["Instance.Save.Import.Picker.Title"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(i18n["Instance.Save.Import.Picker.Type"])
                {
                    Patterns = new[] { "*.mcworld", "*.mctemplate" }
                }
            }
        });

        if (files is { Count: >= 1 })
        {
            var path = files[0].Path.LocalPath;
            if (string.IsNullOrEmpty(path)) return;

            DialogHost.Show(new DialogInfo
            {
                Title = i18n["Instance.Pack.Import.Progress.Title"],
                Content = "正在导入包..."
            });

            var curUser = CurrentUser;
            
            Task.Run(() =>
            {
                if (path.ToLower().EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase))
                {
                    var importer = new ArchiveCheck(VersionInfo);
                    importer.ImportWorldPack(path);
                }
                else if (path.ToLower().EndsWith(".mctemplate", StringComparison.OrdinalIgnoreCase))
                {
                    var importer = new ResourcePackManager(VersionInfo);
                    importer.AddRangePacks(new() { path }, curUser);
                }

                DialogHost.Close();
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    UpdateUI();
                });
            });

            UpdateUI();
        }
    }

    private void TypeSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeSel == null) return;
        UpdateContent();
    }

    private void SavesView_OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null && files.Any(f =>
                f.Name.EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith(".mctemplate", StringComparison.OrdinalIgnoreCase)))
            {
                e.DragEffects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }
        }

        e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    private async void SavesView_OnDrop(object? sender, DragEventArgs e)
    {
        var storageFiles = new List<IStorageFile>();
        foreach (var item in e.DataTransfer.Items)
        {
            if (item.TryGetFile() is IStorageFile file)
            {
                storageFiles.Add(file);
            }
        }

        var validFiles = storageFiles
            .Select(f => f.Path.LocalPath)
            .Where(p => !string.IsNullOrEmpty(p) &&
                (p.EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase) ||
                 p.EndsWith(".mctemplate", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (validFiles.Count == 0) return;

        e.Handled = true;

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Pack.Import.Progress.Title"],
            Content = i18n["Archive.DragDrop.Import.Progress"]
        });

        var curUser = CurrentUser;

        await Task.Run(() =>
        {
            int importedCount = 0;
            foreach (var path in validFiles)
            {
                try
                {
                    if (path.EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase))
                    {
                        var importer = new ArchiveCheck(VersionInfo);
                        importer.ImportWorldPack(path, curUser);
                        importedCount++;
                    }
                    else if (path.EndsWith(".mctemplate", StringComparison.OrdinalIgnoreCase))
                    {
                        var importer = new ResourcePackManager(VersionInfo);
                        importer.AddRangePacks(new List<string> { path }, curUser);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"拖拽导入包失败: {ex.Message}");
                }
            }

            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                DialogHost.Close();
                UpdateUI();
                GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                {
                    Title = i18n["Common.Success"],
                    Message = string.Format(i18n["Archive.DragDrop.Import.Success"], importedCount),
                    NoticeType = NoticeType.Info
                });
            });
        });
    }
}