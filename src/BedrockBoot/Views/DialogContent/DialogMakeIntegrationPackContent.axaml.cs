using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeIntegrationPackContent : UserControl
{
    public DialogMakeIntegrationPackContent()
    {
        InitializeComponent();
    }
    
    public PackInfo PackInfo { get; set; }
    
    public DialogMakeIntegrationPackContent(PackInfo packInfo):this()
    {
        PackInfo = packInfo;
        Make();
    }

    public void Make()
    {
        Task.Run(() =>
        {
            var packer = new IntegrationPackager(PackInfo.VersionConfig);
            packer.IntegrationProgress = new Progress<IntegrationProgress>(progress =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (ProgressBar.IsIndeterminate) ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = (int)progress.Progress;
                    ProgressText.Text = $"({progress.Progress:F2} %) {progress.Message}";
                });
            });
            packer.CompleteCallBack = () => Dispatcher.UIThread.Invoke(() =>
            {
                DialogHost.Close();
                GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                {
                    NoticeType = NoticeType.Info,
                    Message = "整合包导出完毕",
                    Title = "实例"
                });
            });
            packer.BeginPack(PackInfo);
        });
    }
}