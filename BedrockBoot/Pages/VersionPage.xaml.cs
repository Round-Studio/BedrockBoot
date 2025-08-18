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
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using BedrockBoot.Native;
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
        BreadcrumbBar.ItemsSource = new string[] { "管理版本" };
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
                //        Message = "???????",
                //        UseBlueColorForInfo = true,
                //    });
                //}));
                if (!Directory.Exists(Path.Combine(versionInfo.Version_Path, "mods")))
                {
                    Directory.CreateDirectory(Path.Combine(versionInfo.Version_Path, "mods"));
                }

                var packageManager = new PackageManager();
                var findPackages = packageManager.FindPackages();
                bool hasPackage = false;
                foreach (var package in findPackages)
                {
                    if (package.InstalledPath == versionInfo.Version_Path)
                    {
                        hasPackage = true;
                    }
                }
               
                if (hasPackage == true)
                {
                    globalTools.ShowInfo("正在启动中 " + versionInfo.DisPlayName);
                    global_cfg.core.LaunchGame(versionInfo.Type switch
                    {
                        "Release" => VersionType.Release,
                        "Preview" => VersionType.Preview,
                        "Beta" => VersionType.Beta
                    });
                    StartInjectThread(versionInfo.Version_Path);
                    return;
                }
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
                            Debug.WriteLine(exception);
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
                            {
                                MessageBox.ShowAsync(exception.ToString(),"错误");
                            }));
                        } 
                    })
                };
                globalTools.ShowInfo("正更改版本中请耐心等待" + versionInfo.DisPlayName);
                global_cfg.core.RemoveGame(versionInfo.Type switch
                {
                    "Release" => VersionType.Release,
                    "Preview" => VersionType.Preview,
                    "Beta" => VersionType.Beta
                });
                var changeVersion = global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);
                Debug.WriteLine(changeVersion);
                globalTools.ShowInfo("启动中 " + versionInfo.DisPlayName);
                global_cfg.core.LaunchGame(versionInfo.Type switch
                {
                    "Release"=>VersionType.Release,
                    "Preview"=>VersionType.Preview,
                    "Beta"=>VersionType.Beta
                });
              StartInjectThread(versionInfo.Version_Path);
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
            // �����´�����ʾ Mod ����ҳ��
            OpenModManagerWindow(selectedVersion,false);
        }
    }
    private void OpenModManagerWindow(NowVersions version,bool d)
    {
        try
        {
            // ?????????
            var window = new Window();
            window.Title = $"Mod 管理 - {version.DisPlayName}";

            window.ExtendsContentIntoTitleBar = true;
            // ���� Mod ����ҳ��ʵ�������ݲ���
            var modManagerPage = new ModManagerPage(version,d);
            // ���ô�������
            window.Content = modManagerPage;
            IThemeService AppThemeService;
            AppThemeService = new ThemeService(window);
            AppThemeService.AutoInitialize(window);
            AppThemeService.AutoUpdateTitleBarCaptionButtonsColor();
            // ????????
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // ????????
                appWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            }

            // ??????
            window.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.ShowAsync(ex.ToString(), "错误");
        }
    }

    private void StartInjectThread(string path)
    {
        string delay_mods_dir = Path.Combine(path, "d_mods");
        var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
        foreach (var dllFileInfo in dllFileInfos)
        {
            var thread = new Thread(() =>
            {
                WindowsApi.Inject("Minecraft.Windows.exe", dllFileInfo.FullPath, true, 1000);
                globalTools.ShowInfo($"注入 {dllFileInfo.FileName}");
            });
            thread.Start();
        }
    }
    private void DButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions selectedVersion)
        {
            // �����´�����ʾ Mod ����ҳ��
            OpenModManagerWindow(selectedVersion,true);
        }
    }

    private void DeleteButton(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            VersionsList.Remove(versionInfo);
            global_cfg.VersionsList.Remove(versionInfo);
            global_cfg.cfg.SaveVersion(versionInfo);
            globalTools.ShowInfo("已删除");
        }
    }
}
