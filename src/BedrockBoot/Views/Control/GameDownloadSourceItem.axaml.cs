using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Views.Control;

public partial class GameDownloadSourceItem : UserControl
{
    public GameDownloadUrlInfo GameDownloadUrlInfo;
    public Action<int>? Pinged;

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

    public async Task OnPing(int index)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            PingBox.Text = "测试中...";
            PingBox.Background = Brushes.Orange;
        });

        try
        {
            // 使用独立的HttpClient避免复用问题
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                // 使用Stopwatch计时
                var stopwatch = Stopwatch.StartNew();

                // 下载一个固定大小的数据块
                const int bufferSize = 2 * 1024 * 1024;
                var buffer = new byte[8192]; // 8KB chunks

                using (var response = await client.GetAsync(
                           GameDownloadUrlInfo.Url,
                           HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        int totalRead = 0;
                        int read;

                        // 读取1MB数据来测试速度
                        while (totalRead < bufferSize &&
                               (read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            totalRead += read;
                        }

                        stopwatch.Stop();

                        // 计算速度（Bytes/ms -> 转换为合适的单位）
                        double speedInBytesPerSecond = (totalRead * 1000.0) / stopwatch.ElapsedMilliseconds;

                        // 格式化为合适的单位
                        string formattedSpeed;
                        IBrush backgroundColor;

                        if (speedInBytesPerSecond >= 1024 * 1024) // 大于等于1MB/s
                        {
                            double speedMBps = speedInBytesPerSecond / (1024 * 1024);
                            formattedSpeed = $"{speedMBps:F2} MB/s";

                            // 根据速度设置背景色
                            if (speedMBps > 5)
                            {
                                backgroundColor = Brushes.Green;
                            }
                            else if (speedMBps > 1)
                            {
                                backgroundColor = Brushes.Olive;
                            }
                            else
                            {
                                backgroundColor = Brushes.Orange;
                            }
                        }
                        else if (speedInBytesPerSecond >= 1024) // 大于等于1KB/s
                        {
                            double speedKBps = speedInBytesPerSecond / 1024;
                            formattedSpeed = $"{speedKBps:F2} KB/s";
                            backgroundColor = speedKBps > 500 ? Brushes.Olive : Brushes.Orange;
                        }
                        else // 小于1KB/s
                        {
                            formattedSpeed = $"{speedInBytesPerSecond:F0} B/s";
                            backgroundColor = Brushes.OrangeRed;
                        }

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            PingBox.Text = formattedSpeed;
                            PingBox.Background = backgroundColor;
                        });

                        Console.WriteLine($@"速度测试完成: {formattedSpeed}, 耗时: {stopwatch.ElapsedMilliseconds}ms");
                        Pinged?.Invoke(index);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"速度测试错误: {ex.Message}");
            Dispatcher.UIThread.Invoke(() =>
            {
                PingBox.Background = Brushes.DarkRed;
                PingBox.Text = "连接失败";
            });
        }
    }
}