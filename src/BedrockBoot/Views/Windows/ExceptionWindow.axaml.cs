using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Entity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class ExceptionWindow : OnePointWindow
{
    private I18nManager i18n => I18nManager.Instance;

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
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["ExceptionWindow.SaveDialog.Title"],
            SuggestedFileName = Path.GetFileName(Log.FileName),
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(I18nManager.Instance["ExceptionWindow.SaveDialog.FileType"])
                {
                    Patterns = new[] { "*.json" }
                }
            },
            ShowOverwritePrompt = true
        });

        if (file != null)
        {
            var filePath = file.Path.LocalPath;
            File.WriteAllText(filePath, Log.ToJson());
        }
    }
}