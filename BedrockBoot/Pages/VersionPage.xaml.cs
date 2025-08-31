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
using Microsoft.VisualBasic;
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
using BedrockBoot.Integration.Entry;
using WinRT.Interop;

namespace BedrockBoot.Pages;

public sealed partial class VersionPage : Page
{
    // 不再需要 ObservableCollection，改为使用 List 存储数据
    private List<NowVersions> _versionsData = new List<NowVersions>();
    public bool IsEdit = false;
    public bool IsChooseUpdate = false;

    public VersionPage()
    {
        InitializeComponent();

        UpdateUI();
    }

    private void UpdateUI()
    {
        IsEdit = false;
        if (!IsChooseUpdate)
        {
            ChooseGameFolderComboBox.Items.Clear();
            foreach (var x in global_cfg.cfg.JsonCfg.GameFolders)
            {
                ChooseGameFolderComboBox.Items.Add(new ListViewItem()
                {
                    Content = new StackPanel()
                    {
                        Children =
                        {
                            new TextBlock()
                            {
                                Text = $"{x.Name}"
                            },
                            new TextBlock()
                            {
                                Text = $"{x.Path}",
                                TextTrimming = TextTrimming.CharacterEllipsis,
                                FontSize = 9
                            }
                        },
                        Margin = new Thickness(8)
                    }
                });
            }
            ChooseGameFolderComboBox.SelectedIndex = global_cfg.cfg.JsonCfg.ChooseFolderIndex;
        }

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
        IsEdit = true;
    }

    
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            try
            {
                LaunchGameContent.LaunchGame(this.XamlRoot, versionInfo);
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    EasyContentDialog.CreateDialog(this.XamlRoot, "发生了错误", ex.Message);
                });
            }
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
        if (IsEdit)
        {
            IsChooseUpdate = true;
            global_cfg.cfg.JsonCfg.ChooseFolderIndex = ChooseGameFolderComboBox.SelectedIndex;
            global_cfg.cfg.SaveConfig();

            UpdateUI();
            IsChooseUpdate = false;
        }
    }

    // 刷新按钮的方法
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateUI();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Content = new ImportModPackContent(),
            Title = "导入包",
            CloseButtonText = "取消",
            PrimaryButtonText = "开始导入",
            DefaultButton = ContentDialogButton.Primary
        };
        var res = await dialog.ShowAsync();

        if(res == ContentDialogResult.Primary)
        {
            var content = ((ImportModPackContent)dialog.Content);
            if (!string.IsNullOrEmpty(content.PackPath))
            {
                var path = Path.GetDirectoryName(content.Version);
                if (content.PackType == "普通 Mod")
                {
                    path = Path.Combine(path, "mods");
                }
                else if (content.PackType == "延迟加载 Mod")
                {
                    path = Path.Combine(path, "d_mods");
                }

                File.Copy(content.PackPath, Path.Combine(path, Path.GetFileName(content.PackPath)), true);
                EasyContentDialog.CreateDialog(this.XamlRoot, "导入成功", $"已成功将包 {content.PackPath} 导入至游戏。\n目标包类型：{content.PackType}");
            }
            else
            {
                EasyContentDialog.CreateDialog(this.XamlRoot, "值为空", "路径为空");
            }
        }
    }

    private async void AddFolder_OnClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog();

        // 如果 ContentDialog 在桌面应用程序中运行，则必须设置 XamlRoot
        dialog.XamlRoot = this.Content.XamlRoot;
        // dialog.Background = new SolidColorBrush(Colors.Transparent);
        dialog.Content = new AddNewGameFolderContent();
        dialog.XamlRoot = this.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "新增游戏目录";
        dialog.PrimaryButtonText = "新增";
        dialog.CloseButtonText = "取消";
        dialog.DefaultButton = ContentDialogButton.Primary;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var folderpath = ((AddNewGameFolderContent)dialog.Content).FolderPath;
            var foldername = ((AddNewGameFolderContent)dialog.Content).FolderName;
            if (!string.IsNullOrEmpty(folderpath))
            {
                global_cfg.cfg.JsonCfg.GameFolders.Add(new Models.Entry.GameFolderInfoEntry()
                {
                    Name = foldername,
                    Path = folderpath,
                });
                global_cfg.cfg.SaveConfig();
                if (!Directory.Exists(folderpath))
                {
                    Directory.CreateDirectory(folderpath);
                }
                UpdateUI();
            }
        }
    }

    private void SavePack_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NowVersions versionInfo)
        {
            SaveMCIntegrationContent.OpenSave(this.XamlRoot, new VersionOntologyInfo()
            {
                Name = versionInfo.VersionName,
                FolderPath = global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].Path
            });
        }
    }
}