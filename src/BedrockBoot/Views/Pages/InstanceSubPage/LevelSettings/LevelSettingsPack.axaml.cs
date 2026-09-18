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

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsPack : UserControl
{
    private readonly ArchiveInfo? _info;
    public ArchivePackManager Manager { get; private set; }
    public ResourcePackType SelectType => (ResourcePackType)TypeSelBox.SelectedIndex;
    public string SearchKey => SearchBox.Text;

    public LevelSettingsPack()
    {
        InitializeComponent();
    }
    
    public LevelSettingsPack(ArchiveInfo info):this()
    {
        _info = info;
        Manager = new(_info);
        
        UpdateUi();
    }

    public void UpdateUi()
    {
        Manager.Refresh();

        var searchKey = string.IsNullOrEmpty(SearchKey) ? "" : SearchKey;

        var unAct = Manager.GetUnActivatedPacks(SelectType)
            .Where(x => x.Header.Name.Contains(searchKey) || x.Header.Description.Contains(searchKey))
            .ToList();

        var act = Manager.GetActivatedPacks(SelectType)
            .Where(x => x.Header.Name.Contains(searchKey) || x.Header.Description.Contains(searchKey))
            .ToList();

        ActivatedViewer.Children.Clear();
        UnActivatedViewer.Children.Clear();

        act.ForEach(x =>
        {
            ActivatedViewer.Children.Add(new GameArchivePackItem(x, true)
            {
                ActiveAction = (manifest) =>
                {
                    Manager.UninstallPack(manifest);
                    UpdateUi();
                }
            });
        });

        unAct.ForEach(x =>
        {
            UnActivatedViewer.Children.Add(new GameArchivePackItem(x, false)
            {
                ActiveAction = (manifest) =>
                {
                    Manager.InstallPack(manifest);
                    UpdateUi();
                }
            });
        });
    }

    private void TypeSelBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeSelBox != null)
        {
            UpdateUi();
        }
    }

    private void RefreshBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateUi();
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchBox != null)
        {
            UpdateUi();
        }
    }
}