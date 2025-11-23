using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.SettingSubPage;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainSettingPage : BedrockBootPage
{
    public MainSettingPage()
    {
        InitializeComponent();
        
        SettingFrame.NavigateTo(new SettingNavigation());
    }
}