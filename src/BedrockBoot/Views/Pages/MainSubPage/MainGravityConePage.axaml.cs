using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.GravityCone;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.GravityConePage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainGravityConePage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;

    public MainGravityConePage()
    {
        InitializeComponent();
        NavigationFrame = this.MainFrame;

        if (GlobalModel.GravityConeClient == null)
        {
            NavigationFrame.NavigateTo(new GravityConeInit());
        }
        else if (GlobalModel.GravityConeClient != null)
        {
            NavigationFrame.NavigateTo(new GravityConeRoot());
        }
    }
}