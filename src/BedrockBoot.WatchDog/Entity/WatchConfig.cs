namespace BedrockBoot.WatchDog.Entity;

public class WatchConfig
{
    public int GCTestingTimeMs { get; set; } = 500;
    
    /// <summary>
    /// 内存占用阈值（单位：MB）- 监控进程总内存
    /// </summary>
    public long MemoryThresholdMB { get; set; } = 200;
    
    /// <summary>
    /// 监控检查间隔（单位：毫秒）
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = 500;
    
    /// <summary>
    /// 最小 GC 触发间隔（单位：毫秒），避免频繁触发
    /// </summary>
    public int MinGCIntervalMs { get; set; } = 5000;
    
    /// <summary>
    /// 是否总是记录 GC 日志（即使没有释放内存）
    /// </summary>
    public bool AlwaysLogGC { get; set; } = false;
    
    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;
}