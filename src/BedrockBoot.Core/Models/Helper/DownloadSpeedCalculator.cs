using System;
using System.Collections.Generic;
using System.Linq;

namespace BedrockBoot.Models.Helper;

public class DownloadSpeedCalculator
{
    private const int SAMPLE_COUNT = 5; // 采样点数，用于平滑速度计算
    private readonly Queue<(long bytes, DateTime time)> _speedSamples;
    private long _lastBytesReceived;
    private DateTime _lastUpdateTime;

    public DownloadSpeedCalculator()
    {
        _speedSamples = new Queue<(long, DateTime)>();
        Reset();
    }

    public void Reset()
    {
        _lastBytesReceived = 0;
        _lastUpdateTime = DateTime.Now;
        _speedSamples.Clear();
    }

    /// <summary>
    ///     更新下载进度并计算速度
    /// </summary>
    /// <param name="totalBytesReceived">已接收的总字节数</param>
    /// <param name="totalBytesToReceive">总字节数</param>
    /// <returns>下载速度（字节/秒）</returns>
    public double UpdateSpeed(long totalBytesReceived, long totalBytesToReceive)
    {
        var currentTime = DateTime.Now;
        var timeElapsed = (currentTime - _lastUpdateTime).TotalSeconds;

        // 避免除零错误
        if (timeElapsed <= 0)
            return 0;

        // 计算瞬时速度
        var bytesDiff = totalBytesReceived - _lastBytesReceived;
        var instantSpeed = bytesDiff / timeElapsed;

        // 更新状态
        _lastBytesReceived = totalBytesReceived;
        _lastUpdateTime = currentTime;

        // 添加采样点
        _speedSamples.Enqueue((bytesDiff, currentTime));

        // 保持采样点数
        while (_speedSamples.Count > SAMPLE_COUNT) _speedSamples.Dequeue();

        // 计算平均速度
        return CalculateAverageSpeed();
    }

    private double CalculateAverageSpeed()
    {
        if (_speedSamples.Count == 0)
            return 0;

        var totalBytes = _speedSamples.Sum(sample => sample.bytes);
        var timeSpan = (_speedSamples.Last().time - _speedSamples.First().time).TotalSeconds;

        return timeSpan > 0 ? totalBytes / timeSpan : 0;
    }
}