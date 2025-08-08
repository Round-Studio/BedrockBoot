using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using BedrockBoot.Pages.SettingPage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }
    // TODO: 所以什么时候才能写SettingsCard导航？----DM,马上马上
    // BYD,我写了

    private void NavigationTo(SettingsCard sender)
    {
        var Type = sender.Tag as string;
        switch (Type)
        {
            case "Appearance":
                Frame.Navigate(typeof(AppearancePage));
                break;
            case "Behavior":
                Frame.Navigate(typeof(BehaviorPage));
                break;
            case "About":
                Frame.Navigate(typeof(AboutPage));
                break;
            case "DevTools":
                Frame.Navigate(typeof(DevelopersPage));
                break;
            default:
                throw new ArgumentException("Unknown settings card type: " + Type);
        }
    }
}
