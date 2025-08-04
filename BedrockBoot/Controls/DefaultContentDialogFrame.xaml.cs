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
    public sealed partial class DefaultContentDialogFrame : UserControl
    {
        public object? Content { get; set; }
        public string? HeaderIcon { get; set; } = "\uE7B8";
        public string Header { get; set; } = "Title";

        public DefaultContentDialogFrame()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            /// * TODO: 让这个关闭按钮真的能关闭
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
