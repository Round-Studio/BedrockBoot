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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
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

        private async Task<ContentDialogResult> ShowDownloadGameContentDialog(string ver)
        {
            ContentDialog dialog = new ContentDialog();

            // 如果 ContentDialog 在桌面应用程序中运行，则必须设置 XamlRoot
            dialog.XamlRoot = this.Content.XamlRoot;
            // dialog.Background = new SolidColorBrush(Colors.Transparent);
            dialog.Content = new DownloadGameContent();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = ver;
            dialog.PrimaryButtonText = "下载";
            dialog.CloseButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();
            return result;
        }

        private async Task LoadVersionsAsync_()
        {
            try
            {
                //fuck ring 什么鬼ring 搞了我半小时😅👉
                // DM: 用XAML会快一点
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
                    return VersionHelper.GetVersions(
                        "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/raw/main/data.json");
                });

                Task.Run((() =>
                {
                    foreach (var version in versions)
                    {
                        if (string.IsNullOrEmpty(version.ID)) continue;
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
                        _allVersions.ForEach((information =>
                        {
                            VersionItems.Add(information);
                        }));
                    }));
                }));

            }
            catch (Exception ex)
            {
                // 处理错误
            }
        }


        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        

        private Task UpdateUIAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("�޷����ȵ�UI�߳�"));
            }

            return tcs.Task;
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            var showDownloadGameContentDialog = ShowDownloadGameContentDialog((string)(((SettingsCard)sender).Header));
        }

        private void VersionType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VersionItems.Clear();
            string str = VersionType.SelectedIndex switch
            {
                0=>"Release",
                1=>"Preview",
                2=>"Beta",
            };
                    foreach (var version in _allVersions)
                    {
                        if (version.Type == str)
                        {
                    VersionItems.Add(version);
                }
                     
                    }
        }
    }
}