using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Network;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainDownloadPage : BedrockBootPage
{
    private CancellationTokenSource _currentLoadingCancellation = new();
    private string _type = "Release";
    private string _key = "*";
    public bool IsEdit { get; set; } = false;

    public MainDownloadPage()
    {
        InitializeComponent();
        UpdateUI();
        IsEdit = true;
    }

    public async void UpdateUI(string type = "*", string key = "*")
    {
        // 取消之前的加载任务
        _currentLoadingCancellation.Cancel();
        _currentLoadingCancellation = new CancellationTokenSource();
        var cancellationToken = _currentLoadingCancellation.Token;

        // 更新UI状态
        await SetLoadingState(true, false, false);

        try
        {
            await LoadVersionsAsync(type, key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 任务被取消是正常情况，忽略
        }
        catch (Exception ex)
        {
            // 处理其他异常
            Console.WriteLine($@"加载版本列表时出错: {ex.Message}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadingRing.IsVisible = false;
                ScrollViewer.IsVisible = false;
                NoneBox.IsVisible = true;
            });
        }
    }

    private async Task LoadVersionsAsync(string type, string key, CancellationToken cancellationToken)
    {
        Console.WriteLine($@"Version Type: {type} | Key Word: {key}");

        // 在后台线程执行耗时操作
        var (versions, hasItems) = await Task.Run(async () =>
        {
            try
            {
                Console.WriteLine(@"正在加载基岩版版本列表...");
                var lst = VersionHelper.GetVersions(
                    "https://raw.gitcode.com/gcw_lJgzYtGB/-MineCraft-Bedrock-Download-SU/raw/main/bedrock.json");
                Console.WriteLine(@"基岩版版本列表加载完成");

                Console.WriteLine(@"开始序列化");

                // 预处理：为每个项预先计算 Version 对象
                var versionCache = new List<(VersionInformation item, Version? version)>();

                foreach (var item in lst)
                {
                    // 检查取消请求
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(item.ID)) continue;
                    if (item.Variations.Count <= 0) continue;

                    bool isCon = false;

                    foreach (var v in item.Variations)
                    {
                        if (v.UpdateIds.Count <= 0) isCon = true;
                    }

                    if (isCon) continue;

                    Version? version = null;
                    try
                    {
                        version = new Version(item.ID);
                    }
                    catch
                    {
                    }

                    if (item.Type == type || type == "*")
                    {
                        if (key != "*")
                        {
                            if (item.ID.Contains(key))
                                versionCache.Add((item, version));
                        }
                        else
                        {
                            versionCache.Add((item, version));
                        }
                    }
                }

                // 使用缓存的 Version 对象进行排序
                versionCache.Sort((x, y) =>
                {
                    // 两个都有有效版本号
                    if (x.version != null && y.version != null)
                    {
                        return y.version.CompareTo(x.version); // 降序
                    }

                    // 只有一个有有效版本号，有效版本号排在前面
                    if (x.version != null) return -1;
                    if (y.version != null) return 1;

                    // 两个都没有有效版本号，按原始字符串排序
                    return string.Compare(y.item.ID, x.item.ID, StringComparison.Ordinal);
                });

                // 提取排序后的结果
                var sortedList = versionCache.Select(x => x.item).ToList();
                Console.WriteLine(@"序列化完成");

                return (sortedList, sortedList.Count > 0);
            }
            catch (WebException ex)
            {
                Console.WriteLine($@"网络错误: {ex.Message}");
                return (new List<VersionInformation>(), false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"处理版本数据时出错: {ex.Message}");
                return (new List<VersionInformation>(), false);
            }
        });

        // 检查是否被取消
        cancellationToken.ThrowIfCancellationRequested();

        // 在UI线程更新界面
        await UpdateUIAsync(versions, hasItems);
    }

    private async Task UpdateUIAsync(List<VersionInformation> versions, bool hasItems)
    {
        Console.WriteLine(@"开始动态修改 UI");

        // 清空现有项
        ItemsPanel.Children.Clear();

        if (hasItems)
        {
            // 分批加载项，避免一次性添加太多导致UI卡顿
            await AddItemsBatchAsync(versions);
                
            await SetLoadingState(false, true, false);
        }
        else
        {
            await SetLoadingState(false, false, true);
        }

        Console.WriteLine(@"UI 修改完毕");
    }

    private async Task AddItemsBatchAsync(List<VersionInformation> versions)
    {
        const int batchSize = 10; // 每批添加的项目数量
        var totalCount = versions.Count;

        for (int i = 0; i < totalCount; i += batchSize)
        {
            var batch = versions.Skip(i).Take(batchSize).ToList();
            
            // 在UI线程添加一批项目
            foreach (var x in batch)
            {
                var item = new SettingCard()
                {
                    Header = x.ID,
                    Description = string.Join(", ", new string?[]
                    {
                        x.Type,
                        x.Date
                    }),
                    IsClickable = true,
                    Margin = new Thickness(5, 0, 5, 10),
                    IsFontIcon = false,
                    ImageIcon = GetImage("avares://Round.Avalonia.Assets/Image/Icon/Minecraft/草方块.png")
                };

                item.Click += (sender, args) =>
                {
                    GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(), $"下载游戏：{x.ID}");
                };

                ItemsPanel.Children.Add(item);
            }

            // 短暂延迟，让UI有机会更新
            await Task.Delay(10);
        }
    }

    private async Task SetLoadingState(bool loading, bool showScrollViewer, bool showNoneBox)
    {
        LoadingRing.IsVisible = loading;
        ScrollViewer.IsVisible = showScrollViewer;
        NoneBox.IsVisible = showNoneBox;
    }

    public Bitmap GetImage(string url)
    {
        var uri = new Uri(url);

        // 2. 使用 AssetLoader.Open 获取流
        using (var stream = AssetLoader.Open(uri))
        {
            // 3. 将流解码为 Bitmap
            return new Bitmap(stream);
        }
    }

    private void ComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit && ComboBox.SelectedIndex >= 0)
        {
            _type = new string[] { "*","Release", "Preview", "Beta" }[ComboBox.SelectedIndex];
            UpdateUI(_type, _key);
        }
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            _key = TextBox.Text;
            UpdateUI(_type, _key);
        }
    }

    // 清理资源
    public void Dispose()
    {
        _currentLoadingCancellation?.Cancel();
        _currentLoadingCancellation?.Dispose();
    }
}