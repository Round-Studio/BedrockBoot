using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Window = Avalonia.Controls.Window;

namespace BedrockBoot.Views.DrawContent.Chunker;

public partial class DrawChunkerJavaToBedrockContent : UserControl
{
    public DrawChunkerJavaToBedrockContent()
    {
        InitializeComponent();
    }
    private async void ImportArchive_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 Java Edition 存档压缩包",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "压缩文件",
                    Extensions = new List<string> { "zip" }
                }
            }
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dialog.ShowAsync(window);
        
        if (result != null && result.Any())
        {
            string selectedFile = result.First();
            
            string extension = Path.GetExtension(selectedFile).ToLower();
            if (extension.Contains("zip"))
            {
                WorldPath.Text = selectedFile;
            }
            else
            {
                DialogHost.Show(new()
                {
                    Title = "错误的文件",
                    Content = "您选择的文件非 zip 文件",
                    CloseButtonText = "确定"
                });
            }
        }
    }
}