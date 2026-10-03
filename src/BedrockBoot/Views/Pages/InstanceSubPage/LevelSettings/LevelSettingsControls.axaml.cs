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

using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.Archive.Export;
using BedrockBoot.Standard.Enum.Type.Export;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.Archive.RTX;
using BedrockBoot.Views.DialogContent.Export;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsControls : UserControl
{
    public ArchivePackManager Manager { get; private set; }
    private readonly ArchiveInfo _info;
    private bool _isEdit = false;

    public LevelSettingsControls()
    {
        InitializeComponent();
    }

    public LevelSettingsControls(ArchiveInfo info) : this()
    {
        _info = info;
        Manager = new(_info);
        Manager.Refresh();

        if (Manager.ActivatedPacks!.Exists(x => x.Header.Uuid == RTXPack.RTXPackManifest.Header.Uuid))
        {
            RTXPack.ExportPack(_info.VersionInfo);
            IsUseRTX.IsChecked = true;
        }

        _isEdit = true;
    }

    private void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogExportWorldPackContent(_info);
        DialogHost.Show(new()
        {
            Title = "导出存档",
            Content = dialog,
            CloseButtonText = "导出",
            PrimaryButtonText = "取消",
            CloseAction = async () =>
            {
                var conf = new ExportConfig()
                {
                    ExportType = dialog.ArchiveExportType,
                    ArchiveInfo = _info,
                    AllowRandomSeed = dialog.ArchiveAllowRandomSeed,
                    LockTemplateOptions = dialog.ArchiveLockTemplateOptions,
                    PackVersion = dialog.ArchiveVersion,
                    PackName = string.IsNullOrEmpty(dialog.ArchiveName) ? _info.Name : dialog.ArchiveName,
                    PackDescription = dialog.ArchiveDescription
                };
                
                var extension = dialog.ArchiveExportType == ArchiveExportType.World ? "mcworld" : "mctemplate";
                
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null) return;

                var options = new FilePickerSaveOptions
                {
                    Title = "导出存档文件",
                    SuggestedFileName = $"{_info.Name}.{extension}",
                    DefaultExtension = extension,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Minecraft 支持文件") { Patterns = new[] { $"*.{extension}" } }
                    }
                };

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                if (file is not null)
                {
                    var filePath = file.Path.LocalPath;
                    var exp = new ArchiveExport(conf);

                    Task.Run(() =>
                    {
                        DialogHost.Show(new()
                        {
                            Title = "导出中...",
                            Content = "正在导出存档包"
                        });

                        exp.Export(filePath);

                        DialogHost.Close();

                        DialogHost.Show(new()
                        {
                            Title = "导出完毕",
                            Content = $"存档已导出至 {filePath}",
                            CloseButtonText = "确定"
                        });
                    });
                }
            }
        });
    }

    private void IsUseRTX_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isEdit)
        {
            if(IsUseRTX.IsChecked == true)
            {
                RTXPack.ExportPack(_info.VersionInfo);
                Manager.InstallPack(RTXPack.RTXPackManifest);
                RTXPack.InstallPack(_info);
            }
            else
            {
                Manager.UninstallPack(RTXPack.RTXPackManifest);
                RTXPack.RemovePack(_info);
            }
        }
    }
}