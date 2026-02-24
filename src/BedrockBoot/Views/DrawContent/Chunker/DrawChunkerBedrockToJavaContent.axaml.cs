using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Models.Pack.Chunker;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Chunker;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent.Chunker;

public partial class DrawChunkerBedrockToJavaContent : UserControl
{
    public DrawChunkerBedrockToJavaContent()
    {
        InitializeComponent();
    }

    private async void ImportMcWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 Minecraft 世界文件",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "Minecraft World Files",
                    Extensions = new List<string> { "mcworld" }
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

    private void ChooseWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var insChoose = new DialogChooseGameContent();
        DialogHost.Show(new ()
        {
            Title = "选择实例",
            Content = insChoose,
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var worldsChoose = new DialogChooseGameWorldsContent(insChoose.VersionConfig);
                DialogHost.Show(new ()
                {
                    Title = "选择存档",
                    Content = worldsChoose,
                    CloseButtonText = "确定",
                    PrimaryButtonText = "取消",
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = () =>
                    {
                        var arch = worldsChoose.SelectedArchiveInfo;
                        WorldPath.Text = arch!.Path;
                    }
                });
            }
        });
    }

    private async void SaveFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择导出文件夹"
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dialog.ShowAsync(window);

        if (!string.IsNullOrWhiteSpace(result))
        {
            DialogHost.Show(new()
            {
                Title = "转换存档",
                Content = new DialogChunkerConversionContent(
                    ChunkerType.BedrockToJava, 
                    SaveType.Folder,
                    BedrockBoot.Chunker.Chunker.SupportJava[GameVersionChoose.SelectedIndex], 
                    WorldPath.Text!, 
                    result)
            });
        }
    }

    private async void SaveZipFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "保存为 ZIP 文件",
            DefaultExtension = "zip",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "ZIP 压缩文件",
                    Extensions = new List<string> { "zip" }
                }
            }
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dialog.ShowAsync(window);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            DialogHost.Show(new()
            {
                Title = "转换存档",
                Content = new DialogChunkerConversionContent(
                    ChunkerType.BedrockToJava, 
                    SaveType.File,
                    BedrockBoot.Chunker.Chunker.SupportJava[GameVersionChoose.SelectedIndex], 
                    WorldPath.Text!, 
                    result)
            });
        }
    }
}