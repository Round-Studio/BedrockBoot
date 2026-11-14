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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Views.Control.Items.Cape;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesView : UserControl
{
    public CapesPackManager Manager { get; set; }

    public CapesView(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        Manager = new CapesPackManager(versionInfo);
        UpdateContent();
    }

    public CapesView()
    {
        InitializeComponent();
    }

    public VersionConfig VersionInfo { get; set; }
    public Action<CapesPackUnit> EditAction { get; set; }

    private void UpdateContent()
    {
        CapesPackBox.Children.Clear();

        var packs = Manager.LoadPacks();
        NullBox.IsVisible = packs.Count == 0;

        foreach (var pack in packs)
        {
            CapesPackBox.Children.Add(new CapePackItem(pack)
            {
                EditAction = () => EditAction?.Invoke(pack),
                DeleteAction = UpdateContent
            });
        }
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择披风包",
            AllowMultiple = true,
            FileTypeFilter = new[] { new FilePickerFileType("披风包") { Patterns = new[] { "*.mccapes" } } }
        });
        if (files is not { Count: >= 1 }) return;

        var failed = Manager.ImportPacks(files.Select(f => f.Path.LocalPath).ToList());
        UpdateContent();

        if (failed.Count > 0)
            DialogHost.Show(new DialogInfo
            {
                Title = "导入披风包",
                Content = $"以下披风包导入失败（缺少 manifest.json 或文件损坏）：\n{string.Join("\n", failed)}",
                CloseButtonText = "确定"
            });
    }

    private void CreateGroupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
