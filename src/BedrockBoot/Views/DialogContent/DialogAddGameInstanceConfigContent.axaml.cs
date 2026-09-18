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
using Avalonia.Controls;
using BedrockBoot.Core.Global;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameInstanceConfigContent : UserControl
{
    public DialogAddGameInstanceConfigContent()
    {
        InitializeComponent();
    }

    public DialogAddGameInstanceConfigContent(string packName) : this()
    {
        GameName.Text = Path.GetFileName(packName).Replace(".mcpint", "");
        Update();
    }

    public string GameInstallFolder => GlobalModel.Config.Data.GameFolders[GameFolder.SelectedIndex].GameFolderPath;
    public string GameInstallName => GameName.Text;

    public void Update()
    {
        IsEnabled = false;

        GameFolder.Items.Clear();
        GlobalModel.Config.Data.GameFolders.ForEach(f =>
        {
            GameFolder.Items.Add(new ComboBoxItem
            {
                Content = $"{f.GameFolderName} - {f.GameFolderPath}"
            });
        });
        GameFolder.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;

        IsEnabled = true;
    }
}