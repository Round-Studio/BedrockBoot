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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// DM: 没想好这个页面的作用是什么，先留着吧
/// </summary>
public sealed partial class HomePage : Page
{
    public static Action OnVerionPageAction { get; set; }
    public static Action OnSettingsPageAction { get; set; }
    public static Action OnStartAction { get; set; }
   
    public HomePage()
    {
        global_cfg.core.Init();
        InitializeComponent();
       
    }

    private void MainShortcut_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void MainShortcut_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private unsafe void B2_OnClick(object sender, RoutedEventArgs e)
    {
        OnSettingsPageAction();
    }

    private unsafe void B1_OnClick(object sender, RoutedEventArgs e)
    {
        OnVerionPageAction();
    }

    private void HomeButton(object sender, RoutedEventArgs e)
    {
        OnStartAction();
    }
}
