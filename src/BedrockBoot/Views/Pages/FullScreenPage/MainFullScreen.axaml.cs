using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Interface.Page;

namespace BedrockBoot.Views.Pages.FullScreenPage;

public partial class MainFullScreen : UserControl
{
    private readonly IFullScreen _page;

    public MainFullScreen()
    {
        InitializeComponent();
    }

    public MainFullScreen(IFullScreen page) : this()
    {
        _page = page;
        HeaderContent.NavigateTo(page.HeaderContent);
        FullScreenNavigationFrame.NavigateTo(page);
    }

    private void BackHomeBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.FullScreenBack();
    }
}