using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

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

        Console.WriteLine($@"存档：{ArchiveInfo.Name} 路径：{ArchiveInfo.Path}");
        WorldName.Text = ArchiveInfo.Name;
        WorldLastPlayed.Text =
            $"{UnixTimeConverter.UnixTimeStampToDateTime(ArchiveInfo.LevelWorldData.LastPlayed).ToShortDateString()} " +
            $"{UnixTimeConverter.UnixTimeStampToDateTime(ArchiveInfo.LevelWorldData.LastPlayed).ToShortTimeString()}";
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

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine($@"{Directory.Exists(ArchiveInfo.Path)}");
        Process.Start(new ProcessStartInfo
        {
            FileName = ArchiveInfo.Path,
            UseShellExecute = true
        });
    }

    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出 Minecraft World Pack",
            DefaultExtension = "mcworld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("MCWORLD (*.mcworld)")
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });
        
        if (file != null)
        {
            ArchiveInfo.Save(file.TryGetLocalPath());
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
            {
                Title = "成功",
                Message = "存档已导出！",
                NoticeType = NoticeType.Info
            });
        }
    }
}