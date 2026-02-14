using BedrockBoot.Base.Entry;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.SettingSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.Breadcrumb;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainSettingPage : BedrockBootPage
{
    private static NavigationFrame NavigationFrame;
    private static BreadcrumbBar SettingBreadcrumbBar;

    public static void NavigateTo(ISettingPage page)
    {
        NavigationFrame.NavigateTo(page);
        SettingBreadcrumbBar.SetItems(page.BreadcrumbItem);
    }

    public MainSettingPage()
    {
        InitializeComponent();
        SettingBreadcrumbBar = BreadcrumbBar;
        BreadcrumbBar.RootItemClick = () =>
            SettingFrame.NavigateTo(new SettingNavigation());

        NavigationFrame = SettingFrame;

        SettingFrame.NavigateTo(new SettingNavigation());
    }
}