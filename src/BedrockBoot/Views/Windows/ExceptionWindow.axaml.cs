using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Entity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class ExceptionWindow : OnePointWindow
{
    public ExceptionWindow()
    {
        InitializeComponent();
    }

    public ExceptionWindow(ErrorReport logs) : this()
    {
        Log = logs;
        LogBox.Text = logs.Exception.ToString();
    }

    public ErrorReport Log { get; set; }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(Log.Exception.ToString());
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveBtnButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存错误报告",
            SuggestedFileName = Path.GetFileName(Log.FileName),
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                // 定义可选择的文件类型过滤器
                new FilePickerFileType("BedrockBoot 崩溃报告")
                {
                    Patterns = new[] { "*.json" }
                }
            },
            ShowOverwritePrompt = true
        });

        if (file != null)
        {
            var filePath = file.Path.LocalPath;
            File.WriteAllText(filePath,Log.ToJson());
        }
    }
}