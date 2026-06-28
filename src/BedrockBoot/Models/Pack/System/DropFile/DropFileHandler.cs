using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileHandler
{
    private readonly List<string> _files;
    private static I18nManager i18n => I18nManager.Instance;
    
    public DropFileHandler(List<string> files)
    {
        _files = files;
    }

    public void Handle()
    {
        switch (DropFileCheck.GetFileType(_files[0])) // 反正都是同一种文件...所以拿第一个就好了
        {
            case SupportedFileType.Mcaddon:
            case SupportedFileType.Mcpack:
                HandelMcPacks();
                break;
        }
    }

    public void HandelMcPacks()
    {
        Console.WriteLine(@"处理拖入的基岩版支持包文件");
        
        var body = new DialogImportResourcePackContent();

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Pack.Import.Dialog.Title"],
            Content = body,
            CloseButtonText = i18n["Instance.Pack.Import.Dialog.Action"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = async () =>
            {
                var ins = new DialogChooseGameContent();
                DialogHost.Show(new()
                {
                    Title = "选择实例",
                    Content = ins,
                    CloseButtonText = "确定",
                    PrimaryButtonText = "取消",
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = async () =>
                    {
                        DialogHost.Show(new DialogInfo
                        {
                            Title = i18n["Instance.Pack.Import.Progress.Title"],
                            Content = i18n["Instance.Pack.Import.Progress.Content"]
                        });

                        var resourcePackManager = new ResourcePackManager(ins.VersionConfig);

                        // 导入操作在后台执行
                        await Task.Run(() => { resourcePackManager?.AddRangePacks(_files); });

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            DialogHost.Close();
                        });
                    }
                });
            }
        });
        body.Import(_files);
    }
}