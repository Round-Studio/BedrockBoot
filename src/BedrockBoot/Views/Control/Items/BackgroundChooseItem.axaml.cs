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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BedrockBoot.Views.Control.Items;

public partial class BackgroundChooseItem : UserControl
{
    public BackgroundChooseItem()
    {
        InitializeComponent();
    }

    public string ImagePath { get; set; } = string.Empty;

    public void UpdateUI()
    {
        FilePath.Text = ImagePath;
        FileName.Text = Path.GetFileNameWithoutExtension(ImagePath);

        if (File.Exists(ImagePath))
            using (var stream = File.OpenRead(ImagePath))
            {
                ImageBox.Background = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill,
                    Source = Bitmap.DecodeToWidth(stream, 96)
                };
            }

        ImageBox.Child = null;
    }
}