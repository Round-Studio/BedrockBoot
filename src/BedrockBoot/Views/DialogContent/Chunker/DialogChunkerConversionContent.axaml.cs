using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Chunker;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Chunker;

public partial class DialogChunkerConversionContent : UserControl
{
    public DialogChunkerConversionContent()
    {
        InitializeComponent();
    }

    public DialogChunkerConversionContent(
        ChunkerType type,
        SaveType saveType,
        string gameVersion,
        string archivePath,
        string savePath,
        Action<string>? complete = null) : this()
    {
        GlobalModel.MainWindow.CloseDraw();

        Task.Run(() =>
        {
            var chunkerHelper = new ChunkerHelper(type, gameVersion, archivePath,
                BedrockBoot.Chunker.Chunker.DefaultJvmInfo,
                new Progress<double>(p =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProgressText.Text = $"转换中... ({p:F2} %)";
                        ProgressBar.Value = p;
                    });
                }));

            if (saveType == SaveType.File)
                chunkerHelper.ConversionToFile(savePath);
            else
                chunkerHelper.ConversionToFolder(savePath);

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            complete?.Invoke(savePath);
        });
    }
}