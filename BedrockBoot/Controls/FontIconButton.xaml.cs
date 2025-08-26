using Microsoft.UI;
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
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls
{
    public sealed partial class FontIconButton : UserControl
    {
        public string? ShowIcon { get; set; } = "\uE77B";
        public Color TextColor { get; set; } = Colors.Black;

        public Color MainColor { get; set; } = Colors.Transparent;
        private Brush MainBrush => new SolidColorBrush(MainColor);
        public string? PersonName { get; set; }
        public Object? FlyoutContent { get; set; }
        public FontIconButton()
        {
            InitializeComponent();
        }
    }
}
