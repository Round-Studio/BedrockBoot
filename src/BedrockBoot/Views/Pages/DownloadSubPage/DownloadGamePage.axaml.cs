using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadGamePage : UserControl, IDisposable
{
    private CancellationTokenSource? _currentLoadingCancellation;
    private MinecraftGameTypeVersion _currentType = MinecraftGameTypeVersion.Release;
    private string _searchKey = string.Empty;

    public DownloadGamePage()
    {
        InitializeComponent();
        IsEdit = true;

        // 初始加载
        UpdateUI(MinecraftGameTypeVersion.Release);

        Unloaded += (sender, args) => Dispose();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public bool IsEdit { get; set; }

    public void Dispose()
    {
        _currentLoadingCancellation?.Cancel();
        _currentLoadingCancellation?.Dispose();
        _currentLoadingCancellation = null;
        ItemsPanel.Children.Clear();
    }

    /// <summary>
    ///     更新版本列表 UI
    /// </summary>
    public async void UpdateUI(MinecraftGameTypeVersion type, string key = "")
    {
        _currentType = type;
        _searchKey = key;

        // 取消并清理之前的任务
        _currentLoadingCancellation?.Cancel();
        _currentLoadingCancellation?.Dispose();
        _currentLoadingCancellation = new CancellationTokenSource();
        var token = _currentLoadingCancellation.Token;

        try
        {
            // 进入加载状态
            await SetLoadingState(true, false, false);
            ItemsPanel.Children.Clear();

            await LoadVersionsAsync(type, key, token);
        }
        catch (OperationCanceledException)
        {
            /* 忽略取消异常 */
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading versions: {ex.Message}");
            await SetLoadingState(false, false, true);
        }
    }

    private async Task LoadVersionsAsync(MinecraftGameTypeVersion type, string key, CancellationToken token)
    {
        // 1. 在后台线程获取并处理数据
        var processedList = await Task.Run(async () =>
        {
            try
            {
                var sourceIndex = GlobalModel.Config.Data.VersionSourceIndex;
                var source = SourceList.VersionDataSources.ElementAtOrDefault(sourceIndex).Value;

                var buildDatabase = await VersionsHelper.GetBuildDatabaseAsync(source);
                var rawList = await buildDatabase!.Builds.ToListAsync();

                var filtered = new List<(BuildInfo Item, Version? Ver)>();

                foreach (var build in rawList)
                {
                    token.ThrowIfCancellationRequested();

                    var info = build.Value;
                    if (string.IsNullOrEmpty(info.ID) || info.Variations.Count <= 0) continue;

                    // 校验 Metadata 是否存在
                    if (info.Variations.Any(v => v.MetaData.Count <= 0)) continue;

                    // 类型过滤
                    if (info.Type != type) continue;

                    // 关键词过滤
                    if (!string.IsNullOrEmpty(key) &&
                        !info.ID.Contains(key, StringComparison.OrdinalIgnoreCase)) continue;

                    Version? vObj = null;
                    Version.TryParse(info.ID, out vObj);
                    filtered.Add((info, vObj));
                }

                // 2. 排序：版本号降序
                return filtered.OrderByDescending(x => x.Ver).ThenByDescending(x => x.Item.ID).Select(x => x.Item)
                    .ToList();
            }
            catch
            {
                return new List<BuildInfo>();
            }
        }, token);

        token.ThrowIfCancellationRequested();

        // 3. UI 渲染：分批添加防止卡顿
        if (processedList.Count > 0)
        {
            await SetLoadingState(false, true, false);
            await AddItemsBatchAsync(processedList, token);
        }
        else
        {
            await SetLoadingState(false, false, true);
        }
    }

    private async Task AddItemsBatchAsync(List<BuildInfo> versions, CancellationToken token)
    {
        const int batchSize = 12;
        var releaseIcon = "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png";
        var previewIcon = "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";

        for (var i = 0; i < versions.Count; i += batchSize)
        {
            token.ThrowIfCancellationRequested();
            var batch = versions.Skip(i).Take(batchSize).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var x in batch)
                {
                    var iconPath = x.Type == MinecraftGameTypeVersion.Release ? releaseIcon : previewIcon;

                    var card = new SettingCard
                    {
                        Header = x.ID,
                        Description = $"{x.Type} | {x.BuildType} | {x.Date}",
                        IsClickable = true,
                        Margin = new Thickness(5, 0, 5, 10),
                        ImageIcon = GetImage(iconPath)
                    };

                    card.Click += (s, e) =>
                    {
                        var title = $"{i18n["Download.Dialog.TitlePrefix"]}: {x.ID}";
                        Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(x), title);
                    };

                    ItemsPanel.Children.Add(card);
                }
            }, DispatcherPriority.Background);

            // 给 UI 线程喘息时间
            await Task.Delay(5, token);
        }
    }

    private async Task SetLoadingState(bool loading, bool scroll, bool none)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadingRing.IsVisible = loading;
            ScrollViewer.IsVisible = scroll;
            NoneBox.IsVisible = none;
        });
    }

    public Bitmap GetImage(string url)
    {
        using var stream = AssetLoader.Open(new Uri(url));
        return new Bitmap(stream);
    }

    private void ComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit && ComboBox.SelectedIndex >= 0)
            UpdateUI((MinecraftGameTypeVersion)ComboBox.SelectedIndex, _searchKey);
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit) UpdateUI(_currentType, TextBox.Text ?? string.Empty);
    }
}