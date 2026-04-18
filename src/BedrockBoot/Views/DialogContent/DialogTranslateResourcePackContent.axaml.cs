using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Translate;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogTranslateResourcePackContent : UserControl
{
    public DialogTranslateResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogTranslateResourcePackContent(string input, string save) : this()
    {
        Task.Run(() =>
        {
            var translator = new ResourcePackTranslate(new MicrosoftTranslateService());

            translator.TranslatePackageAsync(
                input,
                "zh_CN",
                save,
                (progress, status) =>
                {
                    Console.WriteLine($@"进度: {progress:F2}% - {status}");
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressBar.Value = progress;
                        ProgressText.Text = $"{status} ({progress:F2} %)";
                    });

                    if (progress == 100) Dispatcher.UIThread.InvokeAsync(DialogHost.Close);
                }
            );
        });
    }
}