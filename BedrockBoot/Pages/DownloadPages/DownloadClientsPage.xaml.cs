using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Network;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProgressRing = Microsoft.UI.Xaml.Controls.ProgressRing;

namespace BedrockBoot.Pages.DownloadPages
{
    public sealed partial class DownloadClientsPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private static List<VersionInformation> _versions;
        private static readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationTokenSource;

        public DownloadClientsPage()
        {
            InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _cancellationTokenSource = new CancellationTokenSource();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e) => LoadVersionsAsync().ConfigureAwait(false);

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task LoadVersionsAsync()
        {
            try
            {
                // ��ʾ����״̬
                await UpdateUIAsync(() =>
                {
                    VersionList.Children.Clear();
                    VersionList.Children.Add(new ProgressRing
                    {
                        IsActive = true,
                        Width = 40,
                        Height = 40
                    });
                });

                // ʹ���ź���ȷ���̰߳�ȫ
                await _loadSemaphore.WaitAsync(_cancellationTokenSource.Token);

                try
                {
                    if (_versions == null)
                    {
                        // ʹ��Task.Run�ں�̨�߳�ִ��IO����
                        _versions = await Task.Run(() =>
                        {
                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            return VersionHelper.GetVersions(
                                "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/raw/main/data.json");
                        }, _cancellationTokenSource.Token);

                        _versions?.Reverse();
                    }

                    // ��ʾ����
                    await UpdateUIAsync(() => DisplayVersions(_versions));
                }
                finally
                {
                    _loadSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // ����ȡ������������
            }
            catch (Exception ex)
            {
                await UpdateUIAsync(() =>
                {
                    VersionList.Children.Clear();
                    VersionList.Children.Add(new TextBlock
                    {
                        Text = $"����ʧ��: {ex.Message}",
                        Foreground = (Brush)Application.Current.Resources["SystemErrorTextColor"],
                        Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                    });
                });
            }
        }

        private void DisplayVersions(List<VersionInformation> versions)
        {
            //* 为什么不用ItemRepeater？
            //* ItemRepeater可比直接用ListView更高效，但在某些情况下可能会导致性能问题，尤其是在需要频繁更新UI时。
            //* 但逝这又不会怎么频繁更新UI，为什么不用ItemRepeater？
            // TODO: SPADD -> ItemRepeater
            VersionList.Children.Clear();

            if (versions == null || versions.Count == 0)
            {
                VersionList.Children.Add(new TextBlock
                {
                    Text = "û�п��õİ汾",
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                });
                return;
            }

            foreach (var version in versions)
            {
                if (string.IsNullOrEmpty(version.ID)) continue;

                var card = new SettingsCard
                {
                    Header = version.ID,
                    Description = version.Date,
                    IsClickEnabled = true,
                    Margin = new Thickness(10, 5, 10, 0),
                    Style = (Style)Application.Current.Resources["DefaultSettingsCardStyle"]
                };
                card.Click += (s, e) =>
                {
                    global_cfg._downloadPage.Navigate(new InstallClientPage(), version.ID);
                };

                VersionList.Children.Add(card);
            }
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

        // �ṩ��̬����ˢ������
        public static async Task RefreshVersionsAsync()
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                _versions = null;
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }
    }
}