using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
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

    private void GithubBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Round-Studio/BedrockBoot/issues",
            UseShellExecute = true
        });
    }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = this.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(Log);
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}