using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control;

public partial class GameFolderItem : UserControl
{
    public GameFolderInfo GameFolderInfo { get; set; }
    public GameFolderItem()
    {
        InitializeComponent();
    }
    public GameFolderItem(GameFolderInfo info) : this()
    {
        GameFolderInfo = info;

        FolderPathBox.Text = info.GameFolderPath;
        FolderNameBox.Text = info.GameFolderName;
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("explorer", new[] { GameFolderInfo.GameFolderPath });
    }

    private void DeleteFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "删除目录",
            Content = "请注意，本次删除仅删除启动器中保存的目录，并不会从文件系统上删除其及其子文件。\n您确定要删除吗？",
            SecondaryButtonText = "蒜鸟蒜鸟",
            CloseButtonText = "是是是是是是是",
            AccountButton = DialogButtons.SecondaryButton,
            CloseAction = () =>
            {
                var index = GlobalModel.Config.Data.GameFolders.FindIndex(x =>
                    x.GameFolderPath == GameFolderInfo.GameFolderPath &&
                    x.GameFolderName == GameFolderInfo.GameFolderName);
                
                GlobalModel.Config.Data.GameFolders.RemoveAt(index);
                GlobalModel.Config.Save();
                
                MainManager.Instance.UpdateUI();
            }
        });
    }
}