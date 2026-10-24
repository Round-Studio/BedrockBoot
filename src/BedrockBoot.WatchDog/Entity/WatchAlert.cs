/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace BedrockBoot.WatchDog.Entity;

public enum WatchAlertLevel
{
    Info,
    Warning,
    Critical
}

public enum WatchMetricType
{
    Memory,
    Cpu,
    ThreadPool,
    HandleCount,
    Gen2Heap,
    LargeObjectHeap,
    FinalizationQueue,
    PauseDuration,
    Network,
    DiskIO
}

public class WatchAlertEventArgs : EventArgs
{
    public WatchMetricType MetricType { get; }
    public WatchAlertLevel Level { get; }
    public string Message { get; }
    public double CurrentValue { get; }
    public double? Threshold { get; }
    public DateTime Timestamp { get; }

    public WatchAlertEventArgs(
        WatchMetricType metricType,
        WatchAlertLevel level,
        string message,
        double currentValue,
        double? threshold = null)
    {
        MetricType = metricType;
        Level = level;
        Message = message;
        CurrentValue = currentValue;
        Threshold = threshold;
        Timestamp = DateTime.Now;
    }
}

public class WatchSnapshot
{
    public DateTime Timestamp { get; set; }
    public long WorkingSetMB { get; set; }
    public long PrivateMemoryMB { get; set; }
    public long ManagedMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ThreadPoolPendingWorkItems { get; set; }
    public int ThreadPoolThreadCount { get; set; }
    public int ThreadPoolCompletionPortThreads { get; set; }
    public int HandleCount { get; set; }
    public long Gen0HeapBytes { get; set; }
    public long Gen1HeapBytes { get; set; }
    public long Gen2HeapBytes { get; set; }
    public long LargeObjectHeapBytes { get; set; }
    public long FinalizationQueueCount { get; set; }
    public long PauseDurationMs { get; set; }
    public bool NetworkAvailable { get; set; }
    public double DiskReadMBps { get; set; }
    public double DiskWriteMBps { get; set; }
}
