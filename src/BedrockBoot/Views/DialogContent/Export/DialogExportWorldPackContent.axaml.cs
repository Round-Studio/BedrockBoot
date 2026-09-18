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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum.Type.Export;

namespace BedrockBoot.Views.DialogContent.Export;

public partial class DialogExportWorldPackContent : UserControl
{
    public ArchiveExportType ArchiveExportType => (ArchiveExportType)ExportType.SelectedIndex;
    public string ArchiveName => NameInputBox.Text;
    public string ArchiveDescription => DescriptionInputBox.Text;
    public string ArchiveVersion => VersionInputBox.Text;
    public bool ArchiveAllowRandomSeed => AllowRandomSeed.IsChecked ?? false;
    public bool ArchiveLockTemplateOptions => LockTemplateOptions.IsChecked ?? false;
    private readonly ArchiveInfo _info;

    public DialogExportWorldPackContent()
    {
        InitializeComponent();
    }

    public DialogExportWorldPackContent(ArchiveInfo info) : this()
    {
        _info = info;
        NameInputBox.Text = info.Name;
    }

    private void ExportType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ExportType == null) return;
        TemplateItems.IsVisible = ExportType.SelectedIndex == 1;
    }
}