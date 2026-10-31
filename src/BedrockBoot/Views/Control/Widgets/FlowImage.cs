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
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Widgets;

/// <summary>
///     描述渲染用的异步图片：按 URL 懒加载，加载完成后
///     在不超过容器宽度与高度上限的前提下按原始宽高比定尺寸，绝不拉伸变形。
/// </summary>
public class FlowImage : Border
{
    public static readonly StyledProperty<string?> ImageUrlProperty =
        AvaloniaProperty.Register<FlowImage, string?>(nameof(ImageUrl));

    /// <summary>显示高度上限；超出后按比例缩小。</summary>
    public static readonly StyledProperty<double> MaxDisplayHeightProperty =
        AvaloniaProperty.Register<FlowImage, double>(nameof(MaxDisplayHeight), 380.0);

    private static readonly FontFamily CodeFontFamily = new("Cascadia Mono, Consolas, Courier New");

    private readonly Image _image;
    private readonly FontIcon _placeholder;
    private Bitmap? _bitmap;
    private bool _loadStarted;

    public FlowImage()
    {
        CornerRadius = new CornerRadius(6);
        ClipToBounds = true;
        if (Application.Current?.Resources is { } resources)
            this.Bind(BackgroundProperty, resources.GetResourceObservable("PrimarySubtleBackgroundBrush"));

        _placeholder = new FontIcon
        {
            Glyph = "\uE91B",
            FontSize = 24,
            Opacity = 0.5,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };

        Child = new Grid
        {
            Children = { _placeholder, _image }
        };
    }

    public FlowImage(string url) : this()
    {
        ImageUrl = url;
    }

    public string? ImageUrl
    {
        get => GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public double MaxDisplayHeight
    {
        get => GetValue(MaxDisplayHeightProperty);
        set => SetValue(MaxDisplayHeightProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ImageUrlProperty && !string.IsNullOrEmpty(ImageUrl))
            _ = LoadAsync();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // 布局完成后才知道可用宽度，此时再按宽度决定解码尺寸
        if (ImageUrl != null && !_loadStarted) _ = LoadAsync();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        // 容器宽度变化时重新套用尺寸约束
        if (_bitmap != null) ApplyLayout(_bitmap);
    }

    private async Task LoadAsync()
    {
        if (_loadStarted) return;
        _loadStarted = true;

        var url = ImageUrl;
        try
        {
            var available = Bounds.Width > 10 ? Bounds.Width : 480;
            var decodeWidth = (int)Math.Ceiling(Math.Min(available, 480) * 2);
            var bitmap = await ImageLoader.Shared.LoadIconAsync(url!, decodeWidth);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!ReferenceEquals(ImageUrl, url) || bitmap == null) return;

                _bitmap = bitmap;
                ApplyLayout(bitmap);
                _image.Source = bitmap;
                _placeholder.IsVisible = false;
                Background = null;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load description image: {url} - {ex.Message}");
        }
    }

    private void ApplyLayout(Bitmap bitmap)
    {
        // 用 DPI 把像素尺寸换算成 DIP，再在容器宽度 / 高度上限内按比例缩放（只缩小不放大小图）
        var dpiScale = bitmap.Dpi.X > 0 ? bitmap.Dpi.X / 96.0 : 1.0;
        var naturalWidth = bitmap.PixelSize.Width / dpiScale;
        var naturalHeight = bitmap.PixelSize.Height / dpiScale;
        if (naturalWidth <= 0 || naturalHeight <= 0) return;

        var maxWidth = Bounds.Width > 10 ? Bounds.Width : 480;
        var scale = Math.Min(1.0, Math.Min(maxWidth / naturalWidth, MaxDisplayHeight / naturalHeight));

        _image.Width = Math.Max(1, Math.Floor(naturalWidth * scale));
        _image.Height = Math.Max(1, Math.Floor(naturalHeight * scale));
    }
}
