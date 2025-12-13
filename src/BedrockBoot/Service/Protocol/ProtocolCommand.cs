using System;
using System.IO;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Protocol;

public class ProtocolCommand
{
    public static void OnCommand(string[] command)
    {
        if (command.Contains("import"))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var dialog = new DialogImportGameContent();

                DialogHost.Show(new DialogInfo()
                {
                    Title = "导入游戏安装包",
                    Content = dialog,
                    CloseButtonText = "开始导入",
                    SecondaryButtonText = "取消",
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = () =>
                    {
                        var packPath = dialog.PackFile;
                        var installFolder = dialog.PackInstallFolder;
                        var installName = dialog.PackInstallName;

                        if (string.IsNullOrEmpty(packPath) || !File.Exists(packPath))
                        {
                            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                            {
                                Title = "错误",
                                Message = $"游戏包 {packPath} 无效",
                                NoticeType = NoticeType.Info
                            });
                            return;
                        }

                        if (string.IsNullOrEmpty(installFolder) || !Directory.Exists(installFolder))
                        {
                            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                            {
                                Title = "错误",
                                Message = $"文件夹 {installFolder} 无效",
                                NoticeType = NoticeType.Info
                            });
                            return;
                        }

                        if (string.IsNullOrEmpty(installName))
                        {
                            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                            {
                                Title = "错误",
                                Message = $"请输入有效的实例名称",
                                NoticeType = NoticeType.Info
                            });
                            return;
                        }

                        TaskImportGamePackItem.Install(packPath, installFolder, installName);
                    }
                });
            });
        }else if (command.Contains("about"))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                MainPage.Instance.SelTag.SelectedIndex = 5;
                MainSettingPage.NavigationFrame.NavigateTo(new AboutPage());
            });
        }
    }
}