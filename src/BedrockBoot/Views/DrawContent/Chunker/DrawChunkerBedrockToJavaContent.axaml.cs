using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Views.DialogContent;
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
            WorldPath.Text = selectedFile;
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
}