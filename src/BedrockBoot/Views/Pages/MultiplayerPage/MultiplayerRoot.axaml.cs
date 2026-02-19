using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using PaperConnect.Core.Enum;

namespace BedrockBoot.Views.Pages.MultiplayerPage;

public partial class MultiplayerRoot : UserControl
{
    public MultiplayerRoot()
    {
        InitializeComponent();
    }

    private void CreateRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.PaperConnectCore = new PaperConnectCore()
        {
            EasyTierCliPath = PathsList.EasyTierCliPath,
            EasyTierPath = PathsList.EasyTierCorePath,
            ClientPlayer = "Host",
            GamePort = 7551
        };
        Task.Run(() => GlobalModel.PaperConnectCore.Initialize(CoreType.Server, GlobalModel.ETPublicServer));
        Dispatcher.UIThread.Invoke(() => MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomHost()));
    }
}