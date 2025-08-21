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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using BedrockBoot.Native;
using WinRT.Interop;

namespace BedrockBoot.Pages;

public sealed partial class VersionPage : Page
{
    // 不再需要 ObservableCollection，改为使用 List 存储数据
    private List<NowVersions> _versionsData = new List<NowVersions>();
    public bool IsEdit = false;

    public VersionPage()
    {
        InitializeComponent();
        foreach (var x in global_cfg.cfg.JsonCfg.GameFolders)
        {
            ChooseGameFolderComboBox.Items.Add($"{x.Name} - {x.Path}");
        }
        ChooseGameFolderComboBox.SelectedIndex = global_cfg.cfg.JsonCfg.ChooseFolderIndex;
      //  Loaded += async (s, e) => await LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
    {
        await Task.Run(() => UpdateUI());
    }

    private void UpdateUI()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
        {
            VersionListRepeater.ItemsSource = null;
        }));
        _versionsData.Clear();

        List<string> versionsList = new List<string>();
        var path = global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].Path;
        globalTools.SearchVersionJson(path, ref versionsList, 0, 2);

        // 收集数据
        foreach (var c in versionsList)
        {
            var fullPath = Path.GetFullPath(c);
            try
            {
                var nowVersions = JsonSerializer.Deserialize<NowVersions>(File.ReadAllText(fullPath));
                if (nowVersions != null && !string.IsNullOrEmpty(nowVersions.Type))
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
                    {
                        _versionsData.Add(nowVersions);
                    }));
                   
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载版本 {fullPath} 失败: {ex.Message}");
            }
        }

        // 在 UI 线程上动态创建和添加项
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            // 清空现有的 UI 元素
            VersionListRepeater.ItemsSource = null;

            // 手动创建 UI 元素并添加到 ItemsRepeater
            var items = new List<NowVersions>();
            foreach (var version in _versionsData)
            {
                items.Add(version);
            }
            // 设置 ItemsSource 来触发 UI 更新
            VersionListRepeater.ItemsSource = items;
        });
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            Task.Run((() =>
            {
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
                        if (exception != null)
                        {
                            Debug.WriteLine(exception);
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
                            {
                                MessageBox.ShowAsync(exception.ToString(), "错误");
                            }));
                        }
                    })
                };
                globalTools.ShowInfo("正在注册版本中请耐心等待" + versionInfo.DisPlayName);
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
                    "Release" => VersionType.Release,
                    "Preview" => VersionType.Preview,
                    "Beta" => VersionType.Beta
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
            OpenModManagerWindow(selectedVersion, false);
        }
    }
    private void OpenModManagerWindow(NowVersions version, bool d)
    {
        try
        {
            var window = new Window();
            window.Title = $"Mod 管理 - {version.DisPlayName}";
            window.ExtendsContentIntoTitleBar = true;

            var modManagerPage = new ModManagerPage(version, d);
            window.Content = modManagerPage;

            IThemeService AppThemeService;
            AppThemeService = new ThemeService(window);
            AppThemeService.AutoInitialize(window);
            AppThemeService.AutoUpdateTitleBarCaptionButtonsColor();

            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            }

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
                WindowsApi.Inject("Minecraft.Windows.exe", dllFileInfo.FullPath, true, global_cfg.cfg.JsonCfg.DelayTimes);
                globalTools.ShowInfo($"注入 {dllFileInfo.FileName}");
            });
            thread.Start();
        }
    }

    private void DButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions selectedVersion)
        {
            OpenModManagerWindow(selectedVersion, true);
        }
    }


    private void DeleteButton(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            // 从数据列表中移除
            _versionsData.Remove(versionInfo);

            // 在 UI 线程上重新加载 UI
            DispatcherQueue.TryEnqueue(() =>
            {
                // 重新设置 ItemsSource 来更新 UI
                VersionListRepeater.ItemsSource = _versionsData.ToList();
            });

            try
            {
                File.Delete(versionInfo.Version_Path);
                globalTools.ShowInfo("已删除");
            }
            catch (Exception ex)
            {
                globalTools.ShowInfo($"删除失败: {ex.Message}");
            }
        }
    }

    private void ChooseGameFolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
            global_cfg.cfg.JsonCfg.ChooseFolderIndex = ChooseGameFolderComboBox.SelectedIndex;
            global_cfg.cfg.SaveConfig();
            // 异步重新加载版本列表
            Task.Run(() => UpdateUI());
    }

    // 刷新按钮的方法
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => UpdateUI());
    }

    // 手动添加版本的方法（如果需要）
    private void AddVersionManually(NowVersions version)
    {
        _versionsData.Add(version);

        DispatcherQueue.TryEnqueue(() =>
        {
            // 重新设置 ItemsSource 来更新 UI
            VersionListRepeater.ItemsSource = _versionsData.ToList();
        });
    }

    // 手动移除版本的方法
    private void RemoveVersionManually(NowVersions version)
    {
        _versionsData.Remove(version);

        DispatcherQueue.TryEnqueue(() =>
        {
            // 重新设置 ItemsSource 来更新 UI
            VersionListRepeater.ItemsSource = _versionsData.ToList();
        });
    }
}