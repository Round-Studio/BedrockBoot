using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.CornerSelectBar;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public bool IsEditMode { get; set; } = false;
    public MainPage()
    {
        InitializeComponent();

        IsEditMode = true;
        SelTag_OnSelectionChanged(null, null);
    }

    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var item = (CornerSelectBarItem)SelTag.SelectedItem;
            var tag = item.Tag as string;

            BedrockBootPage page = null;

            switch (tag)
            {
                case "Home":
                    page = new MainHomePage();
                    break;
                case "Manager":
                    page = new MainManager();
                    break;
                case "Download":
                    page = new MainDownloadPage();
                    break;
                case "Task":
                    page = new MainTaskPage();
                    break;
                case "ToolsBox":
                    page = new MainToolsBoxPage();
                    break;
                case "Setting":
                    page = new MainSettingPage();
                    break;
            }

            if (page.HeaderView != null)
            {
                HeaderContent.NavigateTo(page.HeaderView);
            }
            
            MainFrame.NavigateTo(page);
        }
    }
}