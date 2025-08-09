using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Versions;
using BedrockLauncher.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class VersionPage : Page
{
    public ObservableCollection<NowVersions> VersionsList = new ObservableCollection<NowVersions>();

    public VersionPage()
    {
        InitializeComponent();
        BreadcrumbBar.ItemsSource = new string[] { "管理 Minecraft 实例" };
        global_cfg.VersionsList.ForEach((versions =>
        {
            if (string.IsNullOrEmpty(versions.Type))
            {
                return;
            }
            VersionsList.Add(versions);
        }));
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            Task.Run((() =>
            {
                //DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
                //{
                //    Growl.InfoGlobal(new GrowlInfo
                //    {
                //        ShowDateTime = true,
                //        StaysOpen = true,
                //        IsClosable = true,
                //        Title = "Info",
                //        Message = "正在启动",
                //        UseBlueColorForInfo = true,
                //    });
                //}));
             globalTools.ShowInfo("正在启动 " + versionInfo.DisPlayName);
                var installCallback = new InstallCallback()
                {
                    registerProcess_percent = ((s, u) =>
                    {
                        Debug.WriteLine(u);
                    }),
                    result_callback = ((status, exception) =>
                    {
                        if (exception!=null)
                        {
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
                            {
                                MessageBox.ShowAsync("错误", exception.ToString());
                            }));
                        }
                    })
                };
                global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);
                global_cfg.core.LaunchGame(versionInfo.Type switch
                {
                    "Release"=>VersionType.Release,
                    "Preview"=>VersionType.Preview,
                    "Beta"=>VersionType.Beta
                });
            }));
        }
    }

    private void ButtonBaseRem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
          
        }
    }
}
