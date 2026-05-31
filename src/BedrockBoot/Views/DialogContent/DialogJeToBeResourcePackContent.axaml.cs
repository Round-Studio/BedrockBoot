using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Translate;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using ResourcePackConvert.Core.Models;
using ResourcePackConvert.Core.Services;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogJeToBeResourcePackContent : UserControl
{
    public DialogJeToBeResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogJeToBeResourcePackContent(string input, string save) : this()
    {
        var resCon =
            new ResourcePackConvertConverter(
                progress: new Progress<ConversionProgress>(p =>
                {
                    Console.WriteLine($@"进度: {p.Percentage:F2}% - {p.Message}");
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressBar.Value = p.Percentage;
                        ProgressText.Text = $"{p.Stage} ({p.Percentage:F2} %)";
                    });

                    if (p.Percentage == 100) Dispatcher.UIThread.InvokeAsync(DialogHost.Close);
                }));
        
        Task.Run(() =>
        {
            resCon.ConvertResourcePack(input, save);
        });
    }
}