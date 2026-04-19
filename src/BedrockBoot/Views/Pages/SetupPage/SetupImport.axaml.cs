using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Core.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupImport : UserControl
{
    public SetupImport()
    {
        InitializeComponent();
    }

    private void ImportFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo
        {
            // 标题和按钮文本国际化
            Title = I18nManager.Instance["Setup.Import.Dialog.AddFolder.Title"],
            Content = dialog,
            CloseButtonText = I18nManager.Instance["Setup.Import.Dialog.AddFolder.Action"],
            SecondaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    // 逻辑保持不变：如果文件夹名为空，则取路径名
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();
                }
            }
        });
    }

    private void ImportOtherLauncherBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 侧边抽屉标题国际化
        Models.Global.GlobalModel.MainWindow.OpenDraw(
            new DrawImportOtherLauncherContent(),
            I18nManager.Instance["Setup.Import.Draw.ImportOther.Title"]
        );
    }
}