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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Models.Style.Background.AnimationImage;

public class AnimationImageHelper : IDisposable
{
    private readonly string _imagePath;
    private ImageLoader _imageLoader =  new ImageLoader();

    public AnimationImageHelper(string imagePath)
    {
        _imagePath = imagePath;
    }
    
    public async Task<Bitmap?> GetImage(int height = 128)
    {
        var bitmap = await _imageLoader.LoadIconAsync(_imagePath);
        if (bitmap == null)
            return null;
    
        var originalWidth = bitmap.PixelSize.Width;
        var originalHeight = bitmap.PixelSize.Height;
    
        var newWidth = (int)((double)originalWidth * height / originalHeight);
    
        var resizedBitmap = bitmap.CreateScaledBitmap(
            new PixelSize(newWidth, height),
            BitmapInterpolationMode.HighQuality);
    
        bitmap.Dispose();
    
        return resizedBitmap;
    }

    public void Dispose()
    {
        _imageLoader.Dispose();
    }
}