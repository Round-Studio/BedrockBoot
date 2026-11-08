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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Views.Control.Items.Instance.Mods;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ModSupport;

public partial class LArkeShaderPacksManager : UserControl
{
    private readonly VersionConfig _instance;
    public ArkeShaderPacksManager ShaderPackManager { get; set; }

    public LArkeShaderPacksManager()
    {
        InitializeComponent();
    }

    public LArkeShaderPacksManager(VersionConfig instance) : this()
    {
        _instance = instance;
        ShaderPackManager = new ArkeShaderPacksManager(_instance);
        
        UpdateUi();
    }

    public void UpdateUi()
    {
        LoadBox.IsVisible = true;
        NullBox.IsVisible = false;
        ResultBox.IsVisible = false;
        var pkgs = ShaderPackManager.GetShaderPacks();
        if(pkgs.Count == 0)
        {
            NullBox.IsVisible = true;
        }
        else
        {
            NullBox.IsVisible = false;
            
            ResultBox.Children.Clear();
            pkgs.ForEach(pkg =>
            {
                ResultBox.Children.Add(new ArkeShaderPackItem(pkg, (pack) =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = $"删除 {pkg.PackName}",
                        Content = "您确定要删除吗？",
                        CloseButtonText = "确定",
                        PrimaryButtonText = "取消",
                        AccountButton = DialogButtons.CloseButton,
                        CloseAction = () =>
                        {
                            File.Delete(pkg.PackPath);
                            UpdateUi();
                        }
                    });
                }));
            });
            
            ResultBox.IsVisible = true;
        }
        LoadBox.IsVisible = false;
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder.Open(ShaderPackManager.GetShaderPacksDir());
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择光影包文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("光影包文件")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (files.Count == 0) return;

        var targetDir = ShaderPackManager.GetShaderPacksDir();
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in files)
        {
            var localPath = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(localPath)) continue;

            var fileName = Path.GetFileName(localPath);
            var destPath = Path.Combine(targetDir, fileName);

            File.Copy(localPath, destPath, true);
        }

        UpdateUi();
    }
}