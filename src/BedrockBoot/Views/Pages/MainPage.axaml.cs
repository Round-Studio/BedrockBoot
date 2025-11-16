using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public bool IsEditMode { get; set; } = false;
    public MainPage()
    {
        InitializeComponent();

        MainFrame.NavigateTo(new MainHomePage());

        IsEditMode = true;
    }

    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var item = (TabItem)SelTag.SelectedItem;
            var tag = item.Tag as string;

            switch (tag)
            {
                case "Home":
                    MainFrame.NavigateTo(new MainHomePage());
                    break;
                case "Manager":
                    MainFrame.NavigateTo(new MainManager());
                    break;
                case "Download":
                    MainFrame.NavigateTo(new MainDownloadPage());
                    break;
                case "Setting":
                    MainFrame.NavigateTo(new MainSettingPage());
                    break;
            }
        }
    }
}