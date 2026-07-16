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
