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
    }
    public DialogSkinPackViewerContent(string packFolder):this()
    {
        _packFolder = packFolder;
        UpdateUI();
    }

    private async Task UpdateUI()
    {
        var ans = new SkinPackAnalysis(_packFolder);
        _files = ans.GetAllSkin().ToList();

        var items = new List<ItemViewItem>(_files.Count);
        var semaphore = new SemaphoreSlim(5, 5);

        var tasks = _files.Select(async filePath =>
        {
            await semaphore.WaitAsync();
            try
            {
                var image = await Task.Run(() =>
                {
                    using var bitmap = SKBitmap.Decode(filePath);
                    var captured = HeadCapturer.Default.Capture(bitmap);
                    return captured.ToBitmap();
                });
        
                return new ItemViewItem()
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
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var item in results)
        {
            SkinItem.Items.Add(item);
        }

        SkinItem.SelectedIndex = 0;
    }

    private void SkinItem_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var file = _files[SkinItem.SelectedIndex];
        SkinViewer.Skin = new Bitmap(file);
    }

    private double Sensitivity = 5.0;

    private void SkinViewer_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var po = e.GetCurrentPoint(this);
        var pos = e.GetPosition(this);

        var type = PointerType.None;
        if (po.Properties.IsLeftButtonPressed)
            type = PointerType.PointerLeft;
        else if (po.Properties.IsRightButtonPressed) return;

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