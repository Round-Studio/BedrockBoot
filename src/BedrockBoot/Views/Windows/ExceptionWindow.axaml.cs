using Avalonia.Interactivity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class ExceptionWindow : OnePointWindow
{
    public ExceptionWindow()
    {
        InitializeComponent();
    }

    public ExceptionWindow(string logs) : this()
    {
        Log = logs;
        LogBox.Text = logs;
    }

    public string Log { get; set; }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(Log);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}