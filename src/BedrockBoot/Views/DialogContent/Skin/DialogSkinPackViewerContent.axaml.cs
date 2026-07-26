using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BedrockBoot.Models.Pack.Game.ResourcePack.SkinPack;
using LiteSkinViewer2D;
using LiteSkinViewer2D.Extensions;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;
using SkiaSharp;
using PointerType = LiteSkinViewer3D.Shared.Enums.PointerType;

namespace BedrockBoot.Views.DialogContent.Skin;

public partial class DialogSkinPackViewerContent : UserControl
{
    private readonly string _packFolder;
    private List<string> _files;

    public DialogSkinPackViewerContent()
    {
        InitializeComponent();
        
        SkinViewer.PointerMoved += SkinViewer_PointerMoved;
        SkinViewer.PointerPressed += SkinViewer_PointerPressed;
        SkinViewer.PointerReleased += SkinViewer_PointerReleased;
        SkinViewer.PointerWheelChanged += SkinViewer_PointerWheelChanged;
    }
    public DialogSkinPackViewerContent(string packFolder):this()
    {
        _packFolder = packFolder;
        UpdateUI();
    }

    private async Task UpdateUI()
    {
        try
        {
            var ans = new SkinPackAnalysis(_packFolder);
            var files = ans.GetAllSkin().ToList();

            var semaphore = new SemaphoreSlim(5, 5);

            var tasks = files.Select(async filePath =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var image = await Task.Run(() =>
                    {
                        using var bitmap = SKBitmap.Decode(filePath);
                        // 无效 PNG 时 Decode 返回 null，跳过该图片
                        if (bitmap == null) return null;
                        var captured = HeadCapturer.Default.Capture(bitmap);
                        return captured.ToBitmap();
                    });

                    if (image == null) return (filePath, item: (ItemViewItem?)null);

                    return (filePath, item: (ItemViewItem?)new ItemViewItem()
                    {
                        Content = new Border()
                        {
                            Background = new ImageBrush()
                            {
                                Stretch = Stretch.UniformToFill,
                                Source = image
                            },
                            CornerRadius = new CornerRadius(6),
                            Width = 48,
                            Height = 48
                        },
                        Width = 48,
                        Height = 48
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            // 过滤解码失败的图片，并保持 _files 与列表项索引一致
            var valid = results.Where(r => r.item != null).ToList();
            _files = valid.Select(r => r.filePath).ToList();

            foreach (var (_, item) in valid)
            {
                SkinItem.Items.Add(item);
            }

            if (_files.Count > 0)
                SkinItem.SelectedIndex = 0;
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($@"加载皮肤包预览失败: {ex}");
        }
    }

    private void SkinViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        SkinViewer.UpdatePointerWheelChanged(e.Delta.Y > 0);
    }

    private void SkinItem_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_files == null || SkinItem.SelectedIndex < 0 || SkinItem.SelectedIndex >= _files.Count) return;

        var file = _files[SkinItem.SelectedIndex];
        SkinViewer.Skin = file;
    }

    private double Sensitivity = 5.0;

    private void SkinViewer_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pos = e.GetPosition(this);

        // 释放事件时 Properties.IsLeftButtonPressed 恒为 false，需用 InitialPressMouseButton 判断
        var type = PointerType.None;
        if (e.InitialPressMouseButton == MouseButton.Left)
            type = PointerType.PointerLeft;
        else if (e.InitialPressMouseButton == MouseButton.Right) return;

        SkinViewer.UpdatePointerReleased(type, new Vector2((float)((float)pos.X * Sensitivity), (float)((float)pos.Y* Sensitivity)));
    }

    private void SkinViewer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var po = e.GetCurrentPoint(this);
        var pos = e.GetPosition(this);

        var type = PointerType.None;
        if (po.Properties.IsLeftButtonPressed)
            type = PointerType.PointerLeft;
        else if (po.Properties.IsRightButtonPressed) return;

        SkinViewer.UpdatePointerPressed(type, new Vector2((float)((float)pos.X* Sensitivity), (float)((float)pos.Y* Sensitivity)));
    }

    private void SkinViewer_PointerMoved(object? sender, PointerEventArgs e)
    {
        var po = e.GetCurrentPoint(this);
        var pos = e.GetPosition(this);

        var type = PointerType.None;
        if (po.Properties.IsLeftButtonPressed)
            type = PointerType.PointerLeft;
        else if (po.Properties.IsRightButtonPressed) return;

        SkinViewer.UpdatePointerMoved(type, new Vector2((float)((float)pos.X * Sensitivity), (float)((float)pos.Y * Sensitivity)));
    }
}