using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent.Multiplayer;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.GravityConePage;

public partial class GravityConeRoot : UserControl
{
    public GravityConeRoot()
    {
        InitializeComponent();
    }

    private void CreateRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeLoadRoom(RoomType.Host));
    }

    private void LinkRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogMultiplayerLinkRoomContent();
        DialogHost.Show(new()
        {
            Title = "加入房间",
            Content = dialog,
            CloseButtonText = "加入",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.RoomCode))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new()
                    {
                        Title = "不得为空",
                        Message = "联机码不得为空",
                        NoticeType = NoticeType.Error
                    });
                    return;
                }

                MainGravityConePage.NavigationFrame.NavigateTo(new GravityConeLoadRoom(RoomType.Guest,
                    dialog.RoomCode));
            }
        });
    }
}