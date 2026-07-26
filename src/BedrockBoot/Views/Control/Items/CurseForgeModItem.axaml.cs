using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class CurseForgeModItem : UserControl
{
	private ImageLoader _imageLoader = ImageLoader.Shared;
    public CurseForgeModItem()
    {
        InitializeComponent();

        // 作为 DataTemplate 使用时，虚拟化回收/复用容器会重新赋 DataContext
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not CurseForgeResponse.ModData data) return;
            ModData = data;
            _ = UpdateAsync();
        };
    }

    public CurseForgeModItem(CurseForgeResponse.ModData modData) : this()
    {
        ModData = modData;
        // 触发异步更新
        _ = UpdateAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public CurseForgeResponse.ModData ModData { get; set; } = null!;

    /// <summary>
    ///     异步更新 UI 元素
    /// </summary>
    public async Task UpdateAsync()
    {
        // 1. 设置文本信息
        PackName.Text = ModData.Name;

        var authors = string.Join(", ", ModData.Authors.Select(x => x.Name));
        // 格式化描述：作者, 下载量：1,234,567
        Card.Description = $"{authors} | {i18n["Download.CurseForge.Downloads"]}: {ModData.DownloadCount:N0}";

        // 2. 渲染分类标签
        HeaderBox.Children.Clear();
        if (ModData.Categories != null)
            foreach (var cat in ModData.Categories)
                HeaderBox.Children.Add(new LabelBox
                {
                    Text = cat.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0)
                });

        // 3. 复位图标状态后再异步加载（控件被复用时避免残留上一条数据的图标）
        Card.ImageIcon = null;
        await LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        if (ModData.Logo?.ThumbnailUrl == null) return;

        // 记录当前请求对应的数据，避免图片返回时控件已被复用给另一条数据
        var requested = ModData;

        try
        {
            // 修正：使用 await 替代 .Result，防止阻塞 UI 线程或造成死锁
            var image = await _imageLoader.LoadImageBrushAsync(requested.Logo.ThumbnailUrl);

            if (image != null)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!ReferenceEquals(requested, ModData)) return;
                    Card.IsFontIcon = false;
                    Card.ImageIcon = image;
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load CurseForge thumbnail: {ex.Message}");
        }
    }

    /// <summary>
    ///     点击卡片进入资源下载详情页
    /// </summary>
    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        var title = $"{i18n["Download.Action.GetResource"]}: {ModData.Name}";
        GlobalModel.MainWindow.OpenDraw(new DrawDownloadCurseForgeResourceContent(ModData), title);
    }
}