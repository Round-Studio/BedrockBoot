using System.Diagnostics;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Windows.SubWindows;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainToolsBoxPage : BedrockBootPage
{
    public MainToolsBoxPage()
    {
        InitializeComponent();
    }

    private void FoundLoseFilesBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var rfw = new RecoverFilesWindow();
        rfw.ShowDialog(GlobalModel.MainWindow);
    }

    private void FoundLoseFilesBtn_OnClick1(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}