using BedrockBoot.Versions;
using BedrockLauncher.Core;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Riverside;
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
using WinRT.Interop;

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
                if (!Directory.Exists(Path.Combine(versionInfo.Version_Path, "mods")))
                {
                    Directory.CreateDirectory(Path.Combine(versionInfo.Version_Path, "mods"));
                }
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

    private void ModManagerButton(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions selectedVersion)
        {
            // 开启新窗口显示 Mod 管理页面
            OpenModManagerWindow(selectedVersion);
        }
    }
    private void OpenModManagerWindow(NowVersions version)
    {
        try
        {
            // 创建新窗口
            var window = new Window();
            window.Title = $"Mod 管理 - {version.DisPlayName}";

            window.ExtendsContentIntoTitleBar = true;
            // 创建 Mod 管理页面实例并传递参数
            var modManagerPage = new ModManagerPage(version);
            // 设置窗口内容
            window.Content = modManagerPage;
            IThemeService AppThemeService;
            AppThemeService = new ThemeService(window);
            AppThemeService.AutoInitialize(window);
            AppThemeService.AutoUpdateTitleBarCaptionButtonsColor();
            // 设置窗口大小
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // 设置窗口大小
                appWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            }

            // 激活窗口
            window.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.ShowAsync(ex.ToString(), "错误，请截图给开发者");
        }
    }
}
