using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
    private static I18nManager i18n => I18nManager.Instance;
    public PackInfo? PackInfo { get; set; }

    public DialogMakeIntegrationPackContent()
    {
        InitializeComponent();
    }
    
    public DialogMakeIntegrationPackContent(PackInfo packInfo) : this()
    {
        PackInfo = packInfo;
        // 确保在渲染后开始执行
        StartPackaging();
    }

    /// <summary>
    /// 开始打包整合包
    /// </summary>
    public void StartPackaging()
    {
        if (PackInfo == null) return;

        Task.Run(() =>
        {
            try
            {
                var packer = new IntegrationPackager(PackInfo.VersionConfig);
                
                // 进度回调逻辑
                packer.IntegrationProgress = new Progress<IntegrationProgress>(progress =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        // 首次接收到具体进度时关闭不确定状态
                        if (ProgressBar.IsIndeterminate) 
                            ProgressBar.IsIndeterminate = false;

                        ProgressBar.Value = progress.Progress;
                        
                        // 格式化输出：例如 (85.50 %) 正在压缩资源...
                        ProgressText.Text = $"({progress.Progress:F2} %) {progress.Message}";
                    });
                });

                // 完成回调逻辑
                packer.CompleteCallBack = () => Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();
                    
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        NoticeType = NoticeType.Info,
                        Title = i18n["Instance.Title"], // "实例"
                        Message = i18n["Instance.Pack.Export.Success"] // "整合包导出完毕"
                    });
                });

                // 开始执行打包流程
                packer.BeginPack(PackInfo);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        NoticeType = NoticeType.Error,
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = ex.Message
                    });
                });
            }
        });
    }
}