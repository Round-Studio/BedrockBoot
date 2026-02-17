using System;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game.Pack.Screenshots;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Control.Items;

public partial class ScreenshotsItem : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;
    public ScreenshotsInfo? ScreenshotsInfo { get; set; }

    public ScreenshotsItem()
    {
        InitializeComponent();
    }

    public ScreenshotsItem(ScreenshotsInfo info) : this()
    {
        ScreenshotsInfo = info;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (ScreenshotsInfo == null) return;

        var localTime = DateTimeOffset.FromUnixTimeSeconds(ScreenshotsInfo.CaptureTime).ToLocalTime();
        ShotYear.Text = localTime.ToString("yyyy");
        ShotTime.Text = localTime.ToString("MM.dd HH:mm:ss");

        if (!string.IsNullOrEmpty(ScreenshotsInfo.FilePath) && File.Exists(ScreenshotsInfo.FilePath))
        {
            try
            {
                // 使用 using 确保流在使用后关闭，DecodeToWidth 优化内存占用
                using var stream = File.OpenRead(ScreenshotsInfo.FilePath);
                ImageBox.Background = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill,
                    Source = Bitmap.DecodeToWidth(stream, 265)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load screenshot: {ex.Message}");
            }
        }
    }

    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ScreenshotsInfo == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var localTime = DateTimeOffset.FromUnixTimeSeconds(ScreenshotsInfo.CaptureTime).ToLocalTime();
        var fileName = $"{localTime:yyyy.MM.dd HH.mm.ss}.jpg";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = i18n["Archive.Screenshot.Save.Title"],
            SuggestedFileName = fileName,
            DefaultExtension = ".jpg",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(i18n["Archive.Screenshot.Save.FileType"])
                {
                    Patterns = new[] { "*.jpg", "*.jpeg" },
                    MimeTypes = new[] { "image/jpeg" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                // 仅在必要时使用 System.Drawing 进行格式转换，或者直接复制原始文件
                if (Path.GetExtension(ScreenshotsInfo.FilePath).ToLower() == ".jpg" || 
                    Path.GetExtension(ScreenshotsInfo.FilePath).ToLower() == ".jpeg")
                {
                    await using var sourceStream = File.OpenRead(ScreenshotsInfo.FilePath);
                    await sourceStream.CopyToAsync(stream);
                }
                else
                {
                    using var bitmap = new System.Drawing.Bitmap(ScreenshotsInfo.FilePath);
                    bitmap.Save(stream, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save screenshot: {ex.Message}");
            }
        }
    }
}