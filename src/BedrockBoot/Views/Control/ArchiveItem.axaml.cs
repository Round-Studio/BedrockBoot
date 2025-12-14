using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BedrockBoot.Base.Entry.Game.Pack.Archive;

namespace BedrockBoot.Views.Control;

public partial class ArchiveItem : UserControl
{
    public ArchiveInfo ArchiveInfo { get; set; }
    public ArchiveItem()
    {
        InitializeComponent();
    }

    public ArchiveItem(ArchiveInfo info) : this()
    {
        ArchiveInfo = info;

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (ArchiveInfo == null) throw new NullReferenceException();

        Console.WriteLine($"存档：{ArchiveInfo.Name} 路径：{ArchiveInfo.Path}");
        WorldName.Text = ArchiveInfo.Name;
        ProjectLabel.IsVisible = ArchiveInfo.IsProject;
        if (!string.IsNullOrEmpty(ArchiveInfo.IconPath))
        {
            ImageBox.Background = new ImageBrush()
            {
                Stretch = Stretch.UniformToFill,
                Source = new Bitmap(ArchiveInfo.IconPath)
            };
        }
    }
}