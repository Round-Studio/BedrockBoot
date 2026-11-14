using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BedrockBoot.Models.Pack.Game.Capes;

namespace BedrockBoot.Views.Control.Items.Cape;

public partial class CapePackItem : UserControl
{
    private readonly CapesPackUnit _unit;
    public Action? EditAction { get; set; }

    public CapePackItem()
    {
        InitializeComponent();
    }
    public CapePackItem(CapesPackUnit unit):this()
    {
        _unit = unit;
        
        SettingExpander.Header = _unit.SourceManifest?.Data.Header.Name ?? "Local Cape Pack";
        SettingExpander.Description = $"共 {_unit.LoadCapeFile().Count} 个披风文件";
        if (!string.IsNullOrEmpty(_unit.PackIcon))
        {
            if (File.Exists(_unit.PackIcon))
            {
                var bitmapIcon = new Bitmap(_unit.PackIcon);
                SettingExpander.IsFontIcon = false;
                SettingExpander.ImageIcon = bitmapIcon;
            }
        }
    }

    private void EditBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        EditAction?.Invoke();
    }
}