namespace BedrockBoot.WatchDog.Entity;

public class WatchConfig
{
    #region GC / Memory

    public int GCTestingTimeMs { get; set; } = 500;

    public long MemoryThresholdMB { get; set; } = 200;

    public int MonitoringIntervalMs { get; set; } = 500;

    public int MinGCIntervalMs { get; set; } = 5000;

    public bool AlwaysLogGC { get; set; } = false;

    public bool EnableVerboseLogging { get; set; } = false;

    #endregion

    #region CPU Monitoring

    public bool EnableCpuMonitoring { get; set; } = true;

    public int CpuMonitoringIntervalMs { get; set; } = 2000;

    public double CpuWarningThresholdPercent { get; set; } = 80.0;

    public double CpuCriticalThresholdPercent { get; set; } = 95.0;

    public int CpuHighUsageDurationSeconds { get; set; } = 10;

    #endregion

    #region Thread Pool Monitoring

    public bool EnableThreadPoolMonitoring { get; set; } = true;

    public int ThreadPoolMonitoringIntervalMs { get; set; } = 3000;

    public int ThreadPoolStarvationPendingThreshold { get; set; } = 50;

    #endregion

    #region Handle Monitoring

    public bool EnableHandleMonitoring { get; set; } = true;

    public int HandleMonitoringIntervalMs { get; set; } = 5000;

    public int HandleCountWarningThreshold { get; set; } = 10000;

    public int HandleCountCriticalThreshold { get; set; } = 20000;

    #endregion

    #region GC Heap Monitoring

    public bool EnableGcHeapMonitoring { get; set; } = true;

    public int GcHeapMonitoringIntervalMs { get; set; } = 5000;

    public long Gen2HeapWarningThresholdMB { get; set; } = 100;

    public long LargeObjectHeapWarningThresholdMB { get; set; } = 200;

    public int FinalizationQueueWarningThreshold { get; set; } = 1000;

    #endregion

    #region UI Thread Monitoring

    public bool EnableUiThreadMonitoring { get; set; } = true;

    public int UiThreadMonitoringIntervalMs { get; set; } = 3000;

    public int UiThreadMaxPauseMs { get; set; } = 500;

    #endregion

    #region Network Monitoring

    public bool EnableNetworkMonitoring { get; set; } = true;

    public int NetworkMonitoringIntervalMs { get; set; } = 10000;

    public string NetworkPingTarget { get; set; } = "223.5.5.5";

    #endregion

    #region Disk I/O Monitoring

    public bool EnableDiskMonitoring { get; set; } = false;

    public int DiskMonitoringIntervalMs { get; set; } = 10000;

    public double DiskReadWarningMBps { get; set; } = 100;

    public double DiskWriteWarningMBps { get; set; } = 100;

    #endregion

    #region Snapshot

    public bool EnablePeriodicSnapshot { get; set; } = false;

    public int SnapshotIntervalMs { get; set; } = 30000;

    #endregion
}
