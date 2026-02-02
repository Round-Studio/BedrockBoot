using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameFolderSettingItem : UserControl
{
    public GameFolderSettingItem()
    {
        InitializeComponent();
    }

    public GameFolderSettingItem(GameFolderInfo info,Action callBack) : this()
    {
        GameFolderInfo = info;

        Card.Description = info.GameFolderPath;
        Card.Header = info.GameFolderName;
        CallBack = callBack;
    }

    public GameFolderInfo GameFolderInfo { get; set; }
    public Action? CallBack { get; set; }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("explorer", new[] { GameFolderInfo.GameFolderPath });
    }

    private void DeleteFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
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

                CallBack?.Invoke();
            }
        });
    }

    private void SettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var body = new DialogAddGameFolderContent();
        body.FolderName = GameFolderInfo.GameFolderName;
        body.FolderPath = GameFolderInfo.GameFolderPath;
        DialogHost.Show(new DialogInfo
        {
            Title = "设置目录",
            Content = body,
            SecondaryButtonText = "取消",
            CloseButtonText = "保存设置",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var index = GlobalModel.Config.Data.GameFolders.FindIndex(x =>
                    x.GameFolderPath == GameFolderInfo.GameFolderPath &&
                    x.GameFolderName == GameFolderInfo.GameFolderName);

                GlobalModel.Config.Data.GameFolders[index].GameFolderName = body.FolderName;
                GlobalModel.Config.Data.GameFolders[index].GameFolderPath = body.FolderPath;
                
                GlobalModel.Config.Save();

                CallBack?.Invoke();
            }
        });
    }
}