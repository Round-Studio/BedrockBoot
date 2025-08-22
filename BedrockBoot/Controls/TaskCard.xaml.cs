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

namespace BedrockBoot.Controls;

public sealed partial class TaskCard : UserControl
{
    public string Header { get; set; } = "TaskCard";
    public string HeaderIcon { get; set; } = "\uE821";

    public bool IsCloseButtonVisible { get; set; } = true;

    private Visibility _IsCloseButtonVisible { get; set; } = Visibility.Visible;
    public UIElement Content { get; set; }

    private IconElement _HeaderIcon { get; set; } = new FontIcon() { Glyph = "\uF63C" };

    public TaskCard()
    {
        InitializeComponent();
        if (IsCloseButtonVisible)
        {
            _IsCloseButtonVisible = Visibility.Visible;
        }
        else
        {
            _IsCloseButtonVisible = Visibility.Collapsed;
        }

        try
        {
            _HeaderIcon = new FontIcon() { Glyph = HeaderIcon };
        }
        catch (Exception e)
        {
            // Debug.WriteLine($"Error setting HeaderIcon: {e.Message}");
            // TODO: 这里应该有一个日志记录
            _HeaderIcon = new FontIcon() { Glyph = "\uE821" }; // Fallback icon
        }
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
