using BedrockBoot.Controls;
using BedrockBoot.Controls.ContentDialogContent;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Network;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using WinRT;
using ProgressRing = Microsoft.UI.Xaml.Controls.ProgressRing;

namespace BedrockBoot.Pages.DownloadPages
{
    public sealed partial class VersionsShowPages : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private CancellationTokenSource _cancellationTokenSource;
        public ObservableCollection<VersionInformation> VersionItems { get; set; } = new();
        private List<VersionInformation> _allVersions = new();

        public VersionsShowPages()
        {
            InitializeComponent();
            this.DataContext = this;
          _ =  LoadVersionsAsync_();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _cancellationTokenSource = new CancellationTokenSource();
            Unloaded += OnPageUnloaded;
        }

        private async Task<ContentDialogResult> ShowDownloadGameContentDialog(string ver,VersionInformation version)
        {
            ContentDialog dialog = new ContentDialog();

            // 如果 ContentDialog 在桌面应用程序中运行，则必须设置 XamlRoot
            dialog.XamlRoot = this.Content.XamlRoot;
            // dialog.Background = new SolidColorBrush(Colors.Transparent);
            dialog.Content = new DownloadGameContent(ver);
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = ver;
            dialog.PrimaryButtonText = "下载";
            dialog.CloseButtonText = "取消";
            dialog.MinWidth = 800;
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

                string name = ((DownloadGameContent)dialog.Content).Name;
                foreach (var versions in global_cfg.VersionsList)
                {
                    if (versions.VersionName == name || versions.Version_Path == ((DownloadGameContent)dialog.Content).Path)
                    {
                        MessageBox.ShowAsync("错误", "已存在相同版本");
                        return result;
                    }
                }
                global_cfg.InstallTasksAsync(((DownloadGameContent)dialog.Content).Name, ((DownloadGameContent)dialog.Content).Path, ((DownloadGameContent)dialog.Content).BackColor, ((DownloadGameContent)dialog.Content).ImgBack, version);
            }
            else
            {
                return result;
            }

            if (string.IsNullOrEmpty(((DownloadGameContent)dialog.Content).Path) || string.IsNullOrEmpty(((DownloadGameContent)dialog.Content).Name))
            {
                await MessageBox.ShowAsync("错误", "内容不应为空");
            }

            MessageBox.ShowAsync("提示", "已加入任务列表");

            return result;
        }

        private async Task LoadVersionsAsync_()
        {
            try
            {
                // fuck ring 什么鬼ring 搞了我半小时😅👉
                // DM: 用XAML会快一点
                // 傻逼 ↑ 🤣🤣🤣
                // 2025 8 19 见证上方傻逼

                var progressRing = new ProgressRing
                {
                    IsActive = true,
                    Width = 40,
                    Height = 40
                };
                VersionList.ItemsSource = null; 
                (VersionList.Parent as Panel)?.Children.Remove(VersionList);
                (this.Content as Grid)?.Children.Add(progressRing);

              
                var versions = await Task.Run(() =>
                {
                    VersionItems.Clear();
                    try
                    {
                        var list = VersionHelper.GetVersions(
                            "https://raw.gitcode.com/gcw_lJgzYtGB/-MineCraft-Bedrock-Download-SU/raw/main/bedrock.json");
                        return list;
                    }
                    catch (Exception e)
                    {
                        DispatcherQueue.TryEnqueue((DispatcherQueuePriority.High), (() =>
                        {
                            MessageBox.ShowAsync(e.ToString(), "错误");
                        }));
                        return new List<VersionInformation>();
                    }
                   
                });

                Task.Run((() =>
                {
                    foreach (var version in versions)
                    {
                        if (string.IsNullOrEmpty(version.ID) || string.IsNullOrEmpty(version.Date)) continue;
                        _allVersions.Add(version);
                    }
                    _allVersions.Sort((x, y) =>
                    {
                        try
                        {
                            var versionX = new Version(x.ID);
                            var versionY = new Version(y.ID);
                            return versionY.CompareTo(versionX); // 降序：y.CompareTo(x)
                        }
                        catch
                        {
                            return 0;
                        }
                    });
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() =>
                    {
                        (this.Content as Grid)?.Children.Remove(progressRing);
                        VersionType_OnSelectionChanged(null,null);
                    }));
                }));
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue((DispatcherQueuePriority.High), (() =>
                {
                    MessageBox.ShowAsync(ex.ToString(), "错误");
                }));
            }
        }
        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            var showDownloadGameContentDialog = ShowDownloadGameContentDialog((string)(((SettingsCard)sender).Header), (frameworkElement.Tag as VersionInformation));
        }

        private void VersionType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VersionItems.Clear();
            var str = VersionType.SelectedIndex switch
            {
                0 => "Release",
                1 => "Preview",
                2 => "Beta"
            };
            foreach (var version in _allVersions)
                if (version.Type == str)
                    VersionItems.Add(version);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 获取搜索关键词（忽略大小写）
            string searchText = SearchBox?.Text?.Trim() ?? string.Empty;

            // 清空当前显示列表
            VersionItems.Clear();

            // 如果没有输入关键词，则显示当前筛选类型的全部版本
            if (string.IsNullOrEmpty(searchText))
            {
                var currentType = VersionType.SelectedIndex switch
                {
                    0 => "Release",
                    1 => "Preview",
                    2 => "Beta",
                    _ => null
                };

                foreach (var version in _allVersions)
                {
                    if (currentType == null || version.Type == currentType)
                        VersionItems.Add(version);
                }
                return;
            }

            // 根据关键词过滤（匹配 ID 或 Date）
            var filteredVersions = _allVersions.Where(v =>
                (v.ID?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.Date?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false
            ));

            // 添加筛选结果
            foreach (var version in filteredVersions)
            {
                VersionItems.Add(version);
            }
        }
    }
}