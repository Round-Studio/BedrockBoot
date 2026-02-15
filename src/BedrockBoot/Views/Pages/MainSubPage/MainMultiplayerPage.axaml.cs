using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MultiplayerPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainMultiplayerPage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;
    public MainMultiplayerPage()
    {
        InitializeComponent();

        NavigationFrame = MainFrame;

        if (!File.Exists(Path.Combine(PathsList.EasyTierPath, "easytier-windows-x86_64", "easytier-core.exe")) ||
            !File.Exists(Path.Combine(PathsList.EasyTierPath, "easytier-windows-x86_64", "easytier-cli.exe")))
        {
            MainFrame.NavigateTo(new MultiplayerDependenceDownload());
        }
        else
        {
            MainFrame.NavigateTo(new MultiplayerRoot());
        }
    }
}