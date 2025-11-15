using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public MainPage()
    {
        InitializeComponent();

        MainFrame.NavigateTo(new MainHomePage());
    }
}