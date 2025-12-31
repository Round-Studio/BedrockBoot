using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceControls : ISetting
{
    public VersionConfig VersionInfo { get; set; }
    public InstanceControls()
    {
        IsEdit = false;
        
        InitializeComponent();
    }

    public InstanceControls(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "确认删除",
            Content = $"您确定要删除 {VersionInfo.Info.VersionName} ({VersionInfo.Info.Version}) 吗，\n" +
                      $"这将永远无法恢复.jpg",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = (() =>
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = $"删除 {VersionInfo.Info.VersionName}",
                    Content = new DialogDeleteGameContent(VersionInfo)
                });
            }),
            AccountButton = DialogButtons.PrimaryButton
        });
    }
}