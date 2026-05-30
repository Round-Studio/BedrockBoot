using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Views.Control.Items;

public partial class GameDownloadSourceItem : UserControl
{
    public GameDownloadSourceItem()
    {
        InitializeComponent();
    }

    public GameDownloadSourceItem(GameDownloadUrlInfo info) : this()
    {
        GameDownloadUrlInfo = info;
        SourceHost.Text = info.Host;
        SourceUrl.Text = info.Url;
    }

    private static I18nManager i18n => I18nManager.Instance;

    public GameDownloadUrlInfo GameDownloadUrlInfo { get; set; } = null!;
    public Action<int>? Pinged { get; set; }

    /// <summary>
    ///     执行下载速度测试
    /// </summary>
    /// <param name="index">当前源在列表中的索引</param>
    public async Task OnPing(int index)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            PingBox.Text = i18n["Download.Source.Testing"]; // "测试中..."
            PingBox.Background = Brushes.Orange;
        });

        try
        {
            // 对于测速任务，使用短生命周期的 HttpClient 是合理的，但需注意 DNS 缓存
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var stopwatch = Stopwatch.StartNew();
            const int testSize = 1024 * 1024; // 测试读取 1MB 数据
            var buffer = new byte[8192];

            using var response = await client.GetAsync(
                GameDownloadUrlInfo.Url,
                HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var totalRead = 0;
            int read;

            // 循环读取直到达到测试大小或流结束
            while (totalRead < testSize && (read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                totalRead += read;

            stopwatch.Stop();

            // 计算速度并格式化
            UpdateSpeedUI(totalRead, stopwatch.ElapsedMilliseconds, index);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Speed test error for {GameDownloadUrlInfo.Host}: {ex.Message}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PingBox.Background = Brushes.DarkRed;
                PingBox.Text = i18n["Download.Source.Failed"]; // "连接失败"
            });
        }
    }

    private void UpdateSpeedUI(long bytesReceived, long elapsedMs, int index)
    {
        // 计算 Bytes/s (避免除以 0)
        var speedBps = bytesReceived * 1000.0 / Math.Max(elapsedMs, 1);

        string formattedSpeed;
        IBrush color;

        // 速度阶梯判断
        if (speedBps >= 1024 * 1024) // >= 1MB/s
        {
            var mbps = speedBps / (1024 * 1024);
            formattedSpeed = $"{mbps:F2} MB/s";
            color = mbps > 5 ? Brushes.Green : mbps > 1 ? Brushes.Olive : Brushes.Orange;
        }
        else if (speedBps >= 1024) // >= 1KB/s
        {
            var kbps = speedBps / 1024;
            formattedSpeed = $"{kbps:F2} KB/s";
            color = kbps > 500 ? Brushes.Olive : Brushes.Orange;
        }
        else // < 1KB/s
        {
            formattedSpeed = $"{speedBps:F0} B/s";
            color = Brushes.OrangeRed;
        }

        Dispatcher.UIThread.Post(() =>
        {
            PingBox.Text = formattedSpeed;
            PingBox.Background = color;

            // 测速完成回调，父组件可据此选择最优源
            Pinged?.Invoke(index);
        });
    }
}