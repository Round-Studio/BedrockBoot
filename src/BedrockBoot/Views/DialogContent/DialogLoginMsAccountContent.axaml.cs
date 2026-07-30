using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogLoginMsAccountContent : UserControl
{
    private string _code;

    public DialogLoginMsAccountContent()
    {
        InitializeComponent();
    }

    public void SetCopyCode(string code, string link)
    {
        LinkBox.IsVisible = true;
        LinkBtn.NavigateUri = new Uri(link);

        CodeCopyBtn.Content = code;
        CodeCopyBtn.IsEnabled = true;
        _code = code;
    }

    private void CodeCopyBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_code)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            clipboard.SetTextAsync(_code);
        }
    }
}