using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent.Chunker;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Chunker;

public partial class DialogChooseChunkerTypeContent : UserControl
{
    public DialogChooseChunkerTypeContent()
    {
        InitializeComponent();
    }

    private void JavaToBedrock_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close();
        GlobalModel.MainWindow.OpenDraw(new DrawChunkerJavaToBedrockContent(), "Java To Bedrock");
    }

    private void BedrockToJava_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close();
        GlobalModel.MainWindow.OpenDraw(new DrawChunkerBedrockToJavaContent(), "Bedrock To Java");
    }
}