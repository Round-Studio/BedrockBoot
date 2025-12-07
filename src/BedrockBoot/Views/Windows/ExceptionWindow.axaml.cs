using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class ExceptionWindow : OnePointWindow
{
    public string Log { get; set; }
    public ExceptionWindow()
    {
        InitializeComponent();
    }

    public ExceptionWindow(string logs) : this()
    {
        Log = logs;
        LogBox.Text = logs;
    }
}