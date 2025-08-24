using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls
{
    public sealed partial class ToolItem : UserControl
    {
        public string HeaderIcon { get; set; } = "\uE706";
        public string Header { get; set; } = "Tool Item";
        public string Description { get; set; } = "Description";
        public UIElement CardContent { get; set; }
        private RoutedEventHandler Clicks { get; set; }
        public event RoutedEventHandler OnSetting
        {
            add
            {
                Clicks = value;
            }
            remove
            {
                
            }
        }
        public Visibility SettingButtonVisibility { get; set; } = Visibility.Visible;
        private IconElement _HeaderIcon { get; set; } = new FontIcon() { Glyph = "\uE706" };
        public ToolItem()
        {
            InitializeComponent();
            _HeaderIcon = new FontIcon() { Glyph = HeaderIcon };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clicks.Invoke(this, e);
        }
    }
}
