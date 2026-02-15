using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerDependenceDownload : UserControl
{
    public MultiplayerDependenceDownload()
    {
        InitializeComponent();
    }

    private void DownloadBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogDownloadMultiPlayerDependenceContent();
        DialogHost.Show(new ()
        {
            Title = "下载联机依赖文件",
            Content = dialog
        });
    }
}