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
    public HomePage()
    {
        global_cfg.core.Init();
        InitializeComponent();
    }
}
