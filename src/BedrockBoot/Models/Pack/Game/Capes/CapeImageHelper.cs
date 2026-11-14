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
using SkiaSharp;
using SKBlendMode = SkiaSharp.SKBlendMode;
using SKCanvas = SkiaSharp.SKCanvas;
using SKFilterMode = SkiaSharp.SKFilterMode;
using SKImage = SkiaSharp.SKImage;
using SKMipmapMode = SkiaSharp.SKMipmapMode;
using SKPaint = SkiaSharp.SKPaint;
using SKRect = SkiaSharp.SKRect;
using SKSamplingOptions = SkiaSharp.SKSamplingOptions;

namespace BedrockBoot.Models.Pack.Game.Capes;

public class CapeImageHelper
{
    public static void PatchCapeIcon(string source, string target)
    {
        using var sourceBitmap = SKBitmap.Decode(source);
        if (sourceBitmap is null)
            throw new InvalidOperationException($"无法解码图片: {source}");

        var sourceRect = new SKRect(1, 1, 11, 17);

        using var canvasBitmap = new SKBitmap(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(canvasBitmap);
        canvas.Clear(SKColors.Transparent);

        float scale = Math.Min(64f / sourceRect.Width, 64f / sourceRect.Height);
        float destWidth = sourceRect.Width * scale;
        float destHeight = sourceRect.Height * scale;

        float destLeft = (64 - destWidth) / 2f;
        float destTop = (64 - destHeight) / 2f;
        var destRect = new SKRect(destLeft, destTop, destLeft + destWidth, destTop + destHeight);

        var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);

        using var paint = new SKPaint
        {
            IsAntialias = false
        };

        canvas.DrawBitmap(sourceBitmap, sourceRect, destRect, sampling, paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(canvasBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(target);
        data.SaveTo(output);
    }
    public static void PatchCapeElytra(string source, string target)
    {
        using var bitmap = SKBitmap.Decode(source);
        if (bitmap is null)
            throw new InvalidOperationException($"无法解码图片: {source}");

        using var mutable = bitmap.Copy(SKColorType.Bgra8888);
        if (mutable is null)
            throw new InvalidOperationException("无法创建可变位图");

        using var canvas = new SKCanvas(mutable);

        using var paint = new SKPaint
        {
            BlendMode = SKBlendMode.Clear,
            Color = SKColors.Transparent,
            IsAntialias = false
        };

        canvas.DrawRect(SKRect.Create(0, 0, 22, 32), paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(mutable);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(target);
        data.SaveTo(output);
    }
}