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

    /// <summary>显示宽度占容器的百分比上限（0 = 不限制）；对应 HTML 的 width:N% 图片。</summary>
    public static readonly StyledProperty<double> MaxDisplayWidthPercentProperty =
        AvaloniaProperty.Register<FlowImage, double>(nameof(MaxDisplayWidthPercent), 0.0);

    /// <summary>保持原始尺寸不放大（窄长装饰条等按新尺寸显示会失真的图）。</summary>
    public static readonly StyledProperty<bool> KeepNaturalSizeProperty =
        AvaloniaProperty.Register<FlowImage, bool>(nameof(KeepNaturalSize));

    /// <summary>小图放大的宽度上限；超过此宽度的图不参与放大。避免小图标被撑到与横幅一样大。</summary>
    public static readonly StyledProperty<double> MaxUpscaleWidthProperty =
        AvaloniaProperty.Register<FlowImage, double>(nameof(MaxUpscaleWidth), 480.0);

    private static readonly FontFamily CodeFontFamily = new("Cascadia Mono, Consolas, Courier New");

    private readonly Image _image;
    private readonly FontIcon _placeholder;
    private Bitmap? _bitmap;
    private bool _loadStarted;
    // 父布局测量时给的可用宽度。此后一律用它做宽度约束：
    // 若用自身 Bounds，占位图阶段会把约束冻结成几十像素，图片全部显示过小
    private double _availableWidth;

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

    public double MaxDisplayWidthPercent
    {
        get => GetValue(MaxDisplayWidthPercentProperty);
        set => SetValue(MaxDisplayWidthPercentProperty, value);
    }

    public bool KeepNaturalSize
    {
        get => GetValue(KeepNaturalSizeProperty);
        set => SetValue(KeepNaturalSizeProperty, value);
    }

    public double MaxUpscaleWidth
    {
        get => GetValue(MaxUpscaleWidthProperty);
        set => SetValue(MaxUpscaleWidthProperty, value);
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
        // 图片已就绪但还没拿到过可用宽度时，借这次布局完成冻结并定尺寸
        if (_bitmap != null && _availableWidth <= 10)
            ApplyLayout(_bitmap);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 可用宽度从父布局的测量约束取：这是"容器能给多大"，与自己当前期望尺寸无关。
        // 用自身 Bounds 会在占位图阶段冻结成几十像素，把所有图都算小
        if (_availableWidth <= 10 && !double.IsInfinity(availableSize.Width) && availableSize.Width > 10)
            _availableWidth = availableSize.Width;
        return base.MeasureOverride(availableSize);
    }

    private async Task LoadAsync()
    {
        if (_loadStarted) return;
        _loadStarted = true;

        var url = ImageUrl;
        try
        {
            // 解码宽度按 2x 屏余量取，上限 1200（描述横幅图常是 1920 宽，480 的旧上限会显示过小）
            var available = _availableWidth > 10 ? _availableWidth : 700;
            var decodeWidth = (int)Math.Ceiling(Math.Min(available * 2, 1200));
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
        // 用 DPI 把像素尺寸换算成 DIP，再在可用宽度 / 高度上限内按比例缩放
        var dpiScale = bitmap.Dpi.X > 0 ? bitmap.Dpi.X / 96.0 : 1.0;
        var naturalWidth = bitmap.PixelSize.Width / dpiScale;
        var naturalHeight = bitmap.PixelSize.Height / dpiScale;
        if (naturalWidth <= 0 || naturalHeight <= 0) return;

        // 可用宽度只在首次测量时捕获一次；图片加载早于首次测量时等 OnSizeChanged 再来
        if (_availableWidth <= 10) return;

        var maxWidth = _availableWidth;
        if (MaxDisplayWidthPercent > 0)
            maxWidth = Math.Min(maxWidth, maxWidth * MaxDisplayWidthPercent / 100.0);

        // 图片按自身比例适配：大图缩小到可用宽度内，小图放大但有上限
        // （最多放大到 MaxUpscaleWidth，小图标不至于被撑到与横幅一样大）；
        // 窄长装饰条等标记了 KeepNaturalSize 的图只缩不放大
        var upscaleLimit = Math.Min(maxWidth, MaxUpscaleWidth);
        var scale = maxWidth >= naturalWidth
            ? Math.Min(upscaleLimit / naturalWidth, MaxDisplayHeight / naturalHeight)   // 放大：取较宽松的上限
            : Math.Min(maxWidth / naturalWidth, MaxDisplayHeight / naturalHeight);      // 缩小：严格压进约束
        if (KeepNaturalSize) scale = Math.Min(scale, 1.0);

        // 宽高定死在 Border 上：图片加载完成后尺寸不再随测量变化
        Width = Math.Max(1, Math.Floor(naturalWidth * scale));
        Height = Math.Max(1, Math.Floor(naturalHeight * scale));
    }
}
