using System.Diagnostics;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Windows.SubWindows;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

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

    private async void DeleteMcBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new ()
        {
            Title = "卸载游戏中...",
            Content = "卸载完毕将会自动关闭此对话框"
        });

        await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release);
        await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Preview);
        await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Beta);

        DialogHost.Close();
    }
}