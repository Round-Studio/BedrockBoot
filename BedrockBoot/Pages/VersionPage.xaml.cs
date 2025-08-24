using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Models.Classes.Helper;
using BedrockBoot.Models.Classes.Launch;
using BedrockBoot.Native;
using BedrockBoot.Tools;
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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
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

    private void StartInject(string path)
    {
        Task.Run((() =>
        {
            List<DllFileInfo> dllFileInfos = globalTools.GetDllFiles(Path.Combine(path,"d_mods"));
            Process process = null;
            while (true)
            {
               
                var firstOrDefault = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
                if (firstOrDefault == null)
                {
                    continue;
                }
                else
                {
                    process = firstOrDefault;
                    break;
                }
            }
            foreach (var info in dllFileInfos)
            {
                Injector.InjectProcess(process, info.FullPath);
            }
        }));

    }
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            MouseHelper.StopMouseLock();
            MouseHelper.BORDER_MARGIN = global_cfg.cfg.JsonCfg.MouseLockCutPX;

            MouseHelper.AddTargetWindow(versionInfo.VersionName);

            if (global_cfg.cfg.JsonCfg.MouseLock)
            {
                MouseHelper.StartMouseLock();
            }

            Task.Run((() =>
            {
                try
                {
                    int count = 0;
                    if (File.Exists(Path.Combine(versionInfo.Version_Path, "CONCRT140_APP.dll")))
                    {
                        File.Delete(Path.Combine(versionInfo.Version_Path, "CONCRT140_APP.dll"));
                    }
                    bool is_register = false;
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
                        WindowsApi.LoadFix();
                        StartInjectDirect(versionInfo.Version_Path);
                        StartInjectThread(versionInfo.Version_Path);
                        return;
                    }
                    string processName = "Minecraft.Windows"; // 注意：不需要 .exe 扩展名
                    Process[] processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        process.Kill();
                    }

                    bool Launched = false;
                    var installCallback = new InstallCallback()
                    {
                        registerProcess_percent = ((s, u) =>
                        {
                            Debug.WriteLine(u);
                            if (u >= 95)
                            {
                                count++;
                                if (count >= 2 && !Launched)
                                {
                                    global_cfg.core.LaunchGame(versionInfo.Type switch
                                    {
                                        "Release" => VersionType.Release,
                                        "Preview" => VersionType.Preview,
                                        "Beta" => VersionType.Beta
                                    });
                                    WindowsApi.LoadFix();
                                    DispatcherQueue.TryEnqueue((DispatcherQueuePriority.High), (() =>
                                    {
                                        globalTools.ShowInfo("启动中 " + versionInfo.DisPlayName);
                                    }));
                                    StartInjectDirect(versionInfo.Version_Path);
                                    StartInjectThread(versionInfo.Version_Path);
                                    Launched = true;
                                }
                            }
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

                    var _ = global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);

                }
                catch (Exception exception)
                {
                    
                }
                

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
    private async void OpenModManagerWindow(NowVersions version, bool d)
    {
        try
        {
            var window = new ContentDialog();
            window.Title = $"Mod 管理 - {version.DisPlayName}";

            var modManagerPage = new ModManagerPage(version, d);
            window.Content = modManagerPage;
            window.CloseButtonText = "关闭";
            window.XamlRoot = this.XamlRoot;
            await window.ShowAsync();
        }
        catch (Exception ex)
        {
            EasyContentDialog.CreateDialog(this.XamlRoot, "错误", ex.Message);
        }
    }
    public static Process? WaitForMinecraftProcess(int timeoutSec = 60)
    {
        var end = DateTime.Now.AddSeconds(timeoutSec);
        while (DateTime.Now < end)
        {
            var proc = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
            if (proc != null) return proc;
            Thread.Sleep(100);
        }
        return null;
    }

    private void StartInjectThread(string path)
    {
        string delay_mods_dir = Path.Combine(path, "d_mods");
        var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
        Process process = WaitForMinecraftProcess(global_cfg.cfg.JsonCfg.DelayTimes);
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
    private void StartInjectDirect(string path)
    {
        string delay_mods_dir = Path.Combine(path, "mods");
        var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
        Process process = WaitForMinecraftProcess(50);
        foreach (var dllFileInfo in dllFileInfos)
        {
            var thread = new Thread(() =>
            {
                Injector.InjectProcess(process, dllFileInfo.FullPath);
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


    private async void DeleteButton(object sender, RoutedEventArgs e)
    {
        var dialog_ts = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Content = "您确定要删除该版本实例吗，此操作无法撤销！",
            Title = $"确认删除版本实例",
            CloseButtonText = "取消",
            PrimaryButtonText = "确定",
            DefaultButton = ContentDialogButton.Close
        };
        var res = await dialog_ts.ShowAsync();

        if(res == ContentDialogResult.Primary)
        {
            if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
            {
                try
                {
                    // File.Delete(Path.Combine(versionInfo.Version_Path,"version.json"));
                    var dialog = new ContentDialog()
                    {
                        XamlRoot = this.XamlRoot,
                        Content = new DelGameVersionContent(versionInfo.Version_Path),
                        Title = $"删除版本 {versionInfo.VersionName}"
                    };
                    await dialog.ShowAsync();
                    EasyContentDialog.CreateDialog(this.XamlRoot, "删除", "已删除");
                }
                catch (Exception ex)
                {
                    EasyContentDialog.CreateDialog(this.XamlRoot, "删除失败", ex.Message);
                }

                // 从数据列表中移除
                _versionsData.Remove(versionInfo);

                // 在 UI 线程上重新加载 UI
                DispatcherQueue.TryEnqueue(() =>
                {
                    // 重新设置 ItemsSource 来更新 UI
                    VersionListRepeater.ItemsSource = _versionsData.ToList();
                });
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

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Content = new ImportPackContent(),
            Title = "导入包",
            CloseButtonText = "取消",
            PrimaryButtonText = "开始导入",
            DefaultButton = ContentDialogButton.Primary
        };
        var res = await dialog.ShowAsync();

        if(res == ContentDialogResult.Primary)
        {
            var content = ((ImportPackContent)dialog.Content);
            if (!string.IsNullOrEmpty(content.PackPath))
            {
                var path = Path.GetDirectoryName(content.Version);
                bool unzip = false;
                if (content.PackType == "资源包")
                {
                    path = Path.Combine(path, "data", "resource_packs");
                    unzip = true;
                }
                else if (content.PackType == "普通 Mod")
                {
                    path = Path.Combine(path, "mods");
                }
                else if (content.PackType == "延迟加载 Mod")
                {
                    path = Path.Combine(path, "d_mods");
                }

                if (unzip)
                {
                    Directory.CreateDirectory(Path.Combine(path, Path.GetFileName(content.PackPath)));
                    ZipFile.ExtractToDirectory(content.PackPath,Path.Combine(path, Path.GetFileName(content.PackPath)),true);
                }
                else 
                {
                    File.Copy(content.PackPath, Path.Combine(path,Path.GetFileName(content.PackPath)), true);
                }
                EasyContentDialog.CreateDialog(this.XamlRoot, "导入成功", $"已成功将包 {content.PackPath} 导入至游戏。\n目标包类型：{content.PackType}");
            }
            else
            {
                EasyContentDialog.CreateDialog(this.XamlRoot, "值为空", "路径为空");
            }
        }
    }
}