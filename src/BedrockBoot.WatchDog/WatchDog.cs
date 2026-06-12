using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.WatchDog.Entity;

namespace BedrockBoot.WatchDog;

public class WatchDog
{
    private readonly WatchConfig _config;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning;
    private readonly object _lockObject = new object();
    private DateTime _lastGCTime = DateTime.MinValue;

    public WatchDog(WatchConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        lock (_lockObject)
        {
            if (_isRunning)
                throw new InvalidOperationException("WatchDog is already running");

            _isRunning = true;
        }

        Task.Run(() => MonitorMemoryAsync(_cancellationTokenSource.Token));
    }

    public void Stop()
    {
        lock (_lockObject)
        {
            if (!_isRunning)
                return;

            _cancellationTokenSource.Cancel();
            _isRunning = false;
        }
    }

    private async Task MonitorMemoryAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Refresh();
                long workingSetMB = process.WorkingSet64 / (1024 * 1024);
                long privateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);
                long managedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
                long memoryThresholdMB = _config.MemoryThresholdMB;

                // 检查是否超过阈值（使用进程总内存）
                if (workingSetMB >= memoryThresholdMB)
                {
                    // 防止频繁触发 GC（最小间隔 5 秒）
                    if ((DateTime.Now - _lastGCTime).TotalMilliseconds >= _config.MinGCIntervalMs)
                    {
                        long beforeManaged = GC.GetTotalMemory(false);
                        long beforeWorkingSet = workingSetMB;
                        
                        // 强制 GC
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        
                        // 如果是 Server GC，尝试压缩
                        if (GCSettings.IsServerGC)
                        {
                            GC.Collect(2, GCCollectionMode.Forced, true, true);
                        }
                        
                        long afterManaged = GC.GetTotalMemory(true);
                        process.Refresh();
                        long afterWorkingSet = process.WorkingSet64 / (1024 * 1024);
                        
                        long freedManaged = beforeManaged - afterManaged;
                        long freedWorkingSet = beforeWorkingSet - afterWorkingSet;
                        
                        // 只在有内存被释放时输出，或者强制输出
                        if (freedManaged > 0 || freedWorkingSet > 0 || _config.AlwaysLogGC)
                        {
                            Console.WriteLine($"GC executed. " +
                                $"Managed: {beforeManaged}MB -> {afterManaged}MB (freed: {freedManaged}MB), " +
                                $"WorkingSet: {beforeWorkingSet}MB -> {afterWorkingSet}MB (freed: {freedWorkingSet}MB), " +
                                $"Private: {privateMemoryMB}MB");
                        }
                        
                        _lastGCTime = DateTime.Now;
                    }
                    
                    // 等待指定的测试时间
                    await Task.Delay(_config.GCTestingTimeMs, cancellationToken);
                }
                else
                {
                    await Task.Delay(_config.MonitoringIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
    }
}