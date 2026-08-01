using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Widgets;

public partial class LocalImageRenderWidget : UserControl
{
    private ImageLoader _imageLoader = new ImageLoader();
    
    public static readonly StyledProperty<string?> ImageUrlProperty =
        AvaloniaProperty.Register<LocalImageRenderWidget, string?>(nameof(ImageUrl));

    public string? ImageUrl
    {
        get => GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public LocalImageRenderWidget()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public LocalImageRenderWidget(string uri) : this()
    {
        ImageUrl = uri;
    }

    // 在属性发生变化时触发
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ImageUrlProperty)
        {
            var newUrl = change.GetNewValue<string?>();
            if (!string.IsNullOrEmpty(newUrl))
                // 触发异步更新，不阻塞 UI 线程
                _ = Update(newUrl);
        }
    }

    public async Task Update(string uri)
    {
        try
        {
            var image = await _imageLoader.LoadIconAsync(uri);

            // 确保在 UI 线程更新界面
            Dispatcher.UIThread.Post(() =>
            {
                if (image != null)
                {
                    ImageBox.Background = new ImageBrush(image)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                    NoImage.IsVisible = false;
                }
                else
                    // 如果图片加载失败，可以清空背景或设置占位图
                    ImageBox.Background = null;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load image: {ex}");
        }
    }
}