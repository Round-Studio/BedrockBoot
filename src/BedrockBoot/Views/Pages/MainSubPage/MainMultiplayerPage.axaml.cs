using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MultiplayerPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using PaperConnect.Core.Enum;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainMultiplayerPage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;

    public MainMultiplayerPage()
    {
        InitializeComponent();

        NavigationFrame = MainFrame;

        if (!File.Exists(Path.Combine(PathsList.EasyTierPath, "easytier-windows-x86_64", "easytier-core.exe")) ||
            !File.Exists(Path.Combine(PathsList.EasyTierPath, "easytier-windows-x86_64", "easytier-cli.exe")))
        {
            MainFrame.NavigateTo(new MultiplayerDependenceDownload());
        }
        else
        {
            MainFrame.NavigateTo(new MultiplayerRoot());
        }
        
        if (GlobalModel.ETPublicServer == null)
        {
            DialogHost.Show(new DialogInfo()
            {
                Title = "获取节点服务器",
                Content = "正在获取节点服务器..."
            });
            Task.Run(async () =>
            {
                using var client = new HttpClient();

                try
                {
                    string url = "https://et-public-node.roundstudio.top/";
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<string>? nodeList = JsonSerializer.Deserialize<List<string>>(jsonResponse);

                    if (nodeList != null)
                    {
                        Console.WriteLine($"成功获取到 {nodeList.Count} 个节点：");
                        GlobalModel.ETPublicServer = nodeList;
                    }
                    else
                    {
                        Console.WriteLine("反序列化结果为空。");
                        Dispatcher.UIThread.Invoke(() => MainPage.Instance.SelTag.SelectedIndex = 0);
                    }

                    Dispatcher.UIThread.Invoke(DialogHost.Close);
                }
                catch
                {
                    Dispatcher.UIThread.Invoke(DialogHost.Close);
                    Dispatcher.UIThread.Invoke(() => MainPage.Instance.SelTag.SelectedIndex = 0);
                }
            });
        }

        if (GlobalModel.PaperConnectCore != null)
        {
            if (GlobalModel.PaperConnectCore.CoreType == CoreType.Server)
                Dispatcher.UIThread.Invoke(() =>
                    MainMultiplayerPage.NavigationFrame.NavigateTo(new MultiplayerRoomHost()));
        }
    }
}