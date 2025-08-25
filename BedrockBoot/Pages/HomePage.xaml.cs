using BedrockBoot.Controls.ListItem;
using BedrockBoot.Models.Classes.Launch;
using BedrockBoot.Models.Classes.Listen;
using BedrockBoot.Tools;
using BedrockBoot.Versions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Preview.GamesEnumeration;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// DM: 没想好这个页面的作用是什么，先留着吧
/// </summary>
public sealed partial class HomePage : Page
{
    public Thread UpdateVersionThread;
    public bool IsLoad = true;
    public bool IsLaunch = false;
    public static Action OnDownload;
    public HomePage()
    {
        global_cfg.core.Init();
        InitializeComponent();

        UpdateVersionThread = new Thread(UpdateVersion);
        UpdateVersionThread.Start();

        this.Unloaded += new RoutedEventHandler((s, e) =>
        {
            IsLoad = false;
        });
    }
    private NowVersions NowVersion { get; set; }
    private void UpdateVersion()
    {
        while (IsLoad)
        {
            try
            {
                List<string> games = new List<string>();
                globalTools.SearchVersionJson(global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].Path, ref games, 0, 2);

                IsLaunch = true;
                var entry = globalTools.GetJsonFileEntry<NowVersions>(games[global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].SelectVersionIndex]);
                NowVersion = entry;
                DispatcherQueue.TryEnqueue((DispatcherQueuePriority.High), (() =>
                {
                    ChooseVersionName.Text = entry.VersionName;
                    BigLaunchBtnTitle.Text = "启动游戏";
                }));
            }
            catch
            {
                IsLaunch = false;
                DispatcherQueue.TryEnqueue((DispatcherQueuePriority.High), (() =>
                {
                    BigLaunchBtnTitle.Text = "下载游戏";
                    ChooseVersionName.Text = "当前没有游戏可用";
                }));
            }

            Thread.Sleep(200);
        }
    }

    private void LauncherButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (IsLaunch)
        {
            Task.Run(() =>
            {
                try
                {
                    QuickLaunchGame.LaunchGame(NowVersion);
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        EasyContentDialog.CreateDialog(this.XamlRoot, "发生了错误", ex.Message);
                    });
                }
            });
        }
        else
        {
            OnDownload.Invoke();
        }
    }
}
