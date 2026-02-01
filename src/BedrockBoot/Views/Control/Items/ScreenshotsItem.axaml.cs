using System;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game.Pack.Screenshots;

namespace BedrockBoot.Views.Control.Items;

public partial class ScreenshotsItem : UserControl
{
    public ScreenshotsItem()
    {
        InitializeComponent();
    }

    public ScreenshotsItem(ScreenshotsInfo info) : this()
    {
        ScreenshotsInfo = info;

        UpdateUI();
    }

    public ScreenshotsInfo ScreenshotsInfo { get; set; }

    public void UpdateUI()
    {
        if (ScreenshotsInfo == null) throw new NullReferenceException();
        ShotYear.Text = DateTimeOffset.FromUnixTimeSeconds(ScreenshotsInfo.CaptureTime).ToLocalTime().ToString("yyyy");
        ShotTime.Text = DateTimeOffset.FromUnixTimeSeconds(ScreenshotsInfo.CaptureTime).ToLocalTime()
            .ToString("MM.dd hh:mm:ss");

        if (!string.IsNullOrEmpty(ScreenshotsInfo.FilePath))
            ImageBox.Background = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                Source = Bitmap.DecodeToWidth(File.OpenRead(ScreenshotsInfo.FilePath), 265)
            };
    }

    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存图片",
            SuggestedFileName =
                $"{DateTimeOffset.FromUnixTimeSeconds(ScreenshotsInfo.CaptureTime).ToLocalTime().ToString("yyyy.MM.dd hh.mm.ss")}.jpeg",
            DefaultExtension = ".jpeg",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JPEG 图片")
                {
                    Patterns = new[] { "*.jpeg" },
                    MimeTypes = new[] { "image/jpeg" }
                }
            }
        });

        if (file != null)
            try
            {
                await using var stream = await file.OpenWriteAsync();
                new System.Drawing.Bitmap(ScreenshotsInfo.FilePath).Save(stream, ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
            }
    }
}