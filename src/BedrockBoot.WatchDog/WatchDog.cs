using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.WatchDog.Entity;

namespace BedrockBoot.WatchDog;

public class WatchDog : IDisposable
{
    private readonly WatchConfig _config;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning;
    private readonly object _lockObject = new();
    private DateTime _lastGCTime = DateTime.MinValue;
    private readonly ConcurrentQueue<WatchSnapshot> _snapshotBuffer = new();
    private const int MaxSnapshots = 60;
    private readonly Func<Task<bool>>? _uiThreadCheck;

    private DateTime _cpuHighSince;
    private double _lastCpuTime;
    private DateTime _lastCpuSampleTime;
    private bool _cpuHighReported;
    private DateTime _lastDiskSampleTime;
    private double _lastTotalPauseMs;

    /// <summary>
    /// Optional callback to check UI thread responsiveness.
    /// Should return true if UI thread responds within timeout.
    /// </summary>
    public Func<Task<bool>>? UiThreadCheck
    {
        get => _uiThreadCheck;
        init => _uiThreadCheck = value;
    }

    public event EventHandler<WatchAlertEventArgs>? AlertRaised;

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

        var token = _cancellationTokenSource.Token;

        Task.Run(() => MonitorMemoryAsync(token), token);
        if (_config.EnableCpuMonitoring)
            Task.Run(() => MonitorCpuAsync(token), token);
        if (_config.EnableThreadPoolMonitoring)
            Task.Run(() => MonitorThreadPoolAsync(token), token);
        if (_config.EnableHandleMonitoring)
            Task.Run(() => MonitorHandleCountAsync(token), token);
        if (_config.EnableGcHeapMonitoring)
            Task.Run(() => MonitorGcHeapAsync(token), token);
        if (_config.EnableUiThreadMonitoring)
            Task.Run(() => MonitorUiThreadResponsivenessAsync(token), token);
        if (_config.EnableNetworkMonitoring)
            Task.Run(() => MonitorNetworkAsync(token), token);
        if (_config.EnableDiskMonitoring)
            Task.Run(() => MonitorDiskIoAsync(token), token);
        if (_config.EnablePeriodicSnapshot)
            Task.Run(() => SnapshotLoopAsync(token), token);
    }

    public void Stop()
    {
        lock (_lockObject)
        {
            if (!_isRunning) return;
            _cancellationTokenSource.Cancel();
            _isRunning = false;
        }
    }

    public IReadOnlyList<WatchSnapshot> GetRecentSnapshots()
    {
        return _snapshotBuffer.ToArray();
    }

    public WatchSnapshot? GetLatestSnapshot()
    {
        return _snapshotBuffer.TryPeek(out var result) ? result : null;
    }

    private void RaiseAlert(WatchMetricType metric, WatchAlertLevel level, string message, double currentValue, double? threshold = null)
    {
        if (!_config.EnableVerboseLogging && level == WatchAlertLevel.Info) return;

        var args = new WatchAlertEventArgs(metric, level, message, currentValue, threshold);
        Console.WriteLine($@"[WatchDog][{level}] {message}");
        AlertRaised?.Invoke(this, args);
    }

    #region Memory / GC Monitoring

    private async Task MonitorMemoryAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Refresh();
                var workingSetMB = process.WorkingSet64 / (1024 * 1024);
                var privateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);
                var managedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
                var threshold = _config.MemoryThresholdMB;

                if (workingSetMB >= threshold)
                {
                    if ((DateTime.Now - _lastGCTime).TotalMilliseconds >= _config.MinGCIntervalMs)
                    {
                        var beforeManaged = GC.GetTotalMemory(false);
                        var beforeWorkingSet = workingSetMB;

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        if (GCSettings.IsServerGC)
                            GC.Collect(2, GCCollectionMode.Forced, true, true);

                        var afterManaged = GC.GetTotalMemory(true);
                        process.Refresh();
                        var afterWorkingSet = process.WorkingSet64 / (1024 * 1024);

                        var freedManaged = beforeManaged - afterManaged;
                        var freedWorkingSet = beforeWorkingSet - afterWorkingSet;

                        if (_config.AlwaysLogGC)
                            Console.WriteLine(
                                $"GC executed. Managed: {beforeManaged}MB -> {afterManaged}MB (freed: {freedManaged}MB), " +
                                $"WorkingSet: {beforeWorkingSet}MB -> {afterWorkingSet}MB (freed: {freedWorkingSet}MB), " +
                                $"Private: {privateMemoryMB}MB");

                        if (afterWorkingSet >= threshold)
                            RaiseAlert(WatchMetricType.Memory, WatchAlertLevel.Warning,
                                $"Memory still high after GC: {afterWorkingSet}MB (threshold: {threshold}MB)",
                                afterWorkingSet, threshold);

                        _lastGCTime = DateTime.Now;
                    }

                    await Task.Delay(_config.GCTestingTimeMs, cancellationToken);
                }
                else
                {
                    await Task.Delay(_config.MonitoringIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorMemory error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    #endregion

    #region CPU Monitoring

    private async Task MonitorCpuAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();
        _lastCpuTime = process.TotalProcessorTime.Ticks;
        _lastCpuSampleTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.CpuMonitoringIntervalMs, cancellationToken);

                process.Refresh();
                var currentCpuTime = process.TotalProcessorTime.Ticks;
                var currentSampleTime = DateTime.UtcNow;

                var cpuTimeDelta = currentCpuTime - _lastCpuTime;
                var wallClockDelta = (currentSampleTime - _lastCpuSampleTime).Ticks;

                _lastCpuTime = currentCpuTime;
                _lastCpuSampleTime = currentSampleTime;

                if (wallClockDelta <= 0) continue;

                var cpuPercent = (double)cpuTimeDelta / wallClockDelta * 100;
                cpuPercent = Math.Round(cpuPercent, 1);

                if (cpuPercent >= _config.CpuCriticalThresholdPercent)
                {
                    if (_cpuHighSince == default)
                        _cpuHighSince = DateTime.Now;

                    var duration = (DateTime.Now - _cpuHighSince).TotalSeconds;
                    if (duration >= _config.CpuHighUsageDurationSeconds && !_cpuHighReported)
                    {
                        _cpuHighReported = true;
                        RaiseAlert(WatchMetricType.Cpu, WatchAlertLevel.Critical,
                            $"CPU usage critically high: {cpuPercent}% for {duration:F0}s (threshold: {_config.CpuCriticalThresholdPercent}%)",
                            cpuPercent, _config.CpuCriticalThresholdPercent);
                    }
                    else if (cpuPercent >= _config.CpuWarningThresholdPercent && duration >= _config.CpuHighUsageDurationSeconds / 2)
                    {
                        RaiseAlert(WatchMetricType.Cpu, WatchAlertLevel.Warning,
                            $"CPU usage high: {cpuPercent}% (warning threshold: {_config.CpuWarningThresholdPercent}%)",
                            cpuPercent, _config.CpuWarningThresholdPercent);
                    }
                }
                else
                {
                    _cpuHighSince = default;
                    _cpuHighReported = false;
                }

                if (_config.EnableVerboseLogging && cpuPercent > 10)
                    Console.WriteLine($@"[WatchDog] CPU: {cpuPercent}%");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorCpu error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    #endregion

    #region Thread Pool Monitoring

    private static int GetPendingWorkItems()
    {
        ThreadPool.GetAvailableThreads(out var workerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
        return maxWorkerThreads - workerThreads;
    }

    private async Task MonitorThreadPoolAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.ThreadPoolMonitoringIntervalMs, cancellationToken);

                var pending = GetPendingWorkItems();
                var threadCount = ThreadPool.ThreadCount;

                if (pending >= _config.ThreadPoolStarvationPendingThreshold)
                    RaiseAlert(WatchMetricType.ThreadPool, WatchAlertLevel.Warning,
                        $"Thread pool starvation detected: {pending} pending work items, {threadCount} threads (threshold: {_config.ThreadPoolStarvationPendingThreshold})",
                        pending, _config.ThreadPoolStarvationPendingThreshold);

                if (_config.EnableVerboseLogging && pending > 10)
                    Console.WriteLine($@"[WatchDog] ThreadPool: {pending} pending, {threadCount} threads");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorThreadPool error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    #endregion

    #region Handle Count Monitoring

    private async Task MonitorHandleCountAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.HandleMonitoringIntervalMs, cancellationToken);

                process.Refresh();
                var handleCount = process.HandleCount;

                if (handleCount >= _config.HandleCountCriticalThreshold)
                    RaiseAlert(WatchMetricType.HandleCount, WatchAlertLevel.Critical,
                        $"Handle count critically high: {handleCount} (threshold: {_config.HandleCountCriticalThreshold})",
                        handleCount, _config.HandleCountCriticalThreshold);
                else if (handleCount >= _config.HandleCountWarningThreshold)
                    RaiseAlert(WatchMetricType.HandleCount, WatchAlertLevel.Warning,
                        $"Handle count high: {handleCount} (warning threshold: {_config.HandleCountWarningThreshold})",
                        handleCount, _config.HandleCountWarningThreshold);

                if (_config.EnableVerboseLogging && handleCount > 1000)
                    Console.WriteLine($@"[WatchDog] Handles: {handleCount}");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorHandleCount error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    #endregion

    #region GC Heap / LOH / Finalization Queue Monitoring

    private async Task MonitorGcHeapAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.GcHeapMonitoringIntervalMs, cancellationToken);

                var memoryInfo = GC.GetGCMemoryInfo();
                var genInfo = memoryInfo.GenerationInfo;

                var gen0 = genInfo.Length > 0 ? genInfo[0].SizeAfterBytes : 0L;
                var gen1 = genInfo.Length > 1 ? genInfo[1].SizeAfterBytes : 0L;
                var gen2 = genInfo.Length > 2 ? genInfo[2].SizeAfterBytes : 0L;
                var lohBytes = memoryInfo.HeapSizeBytes - gen0 - gen1 - gen2;
                if (lohBytes < 0) lohBytes = 0;

                var gen2MB = gen2 / (1024 * 1024);
                var lohMB = lohBytes / (1024 * 1024);

                if (gen2MB >= _config.Gen2HeapWarningThresholdMB)
                    RaiseAlert(WatchMetricType.Gen2Heap, WatchAlertLevel.Warning,
                        $"Gen 2 heap large: {gen2MB}MB (threshold: {_config.Gen2HeapWarningThresholdMB}MB)",
                        gen2MB, _config.Gen2HeapWarningThresholdMB);

                if (lohMB >= _config.LargeObjectHeapWarningThresholdMB)
                    RaiseAlert(WatchMetricType.LargeObjectHeap, WatchAlertLevel.Warning,
                        $"LOH large: {lohMB}MB (threshold: {_config.LargeObjectHeapWarningThresholdMB}MB)",
                        lohMB, _config.LargeObjectHeapWarningThresholdMB);

                var totalPause = GC.GetTotalPauseDuration().TotalMilliseconds;
                if (totalPause > 100)
                    RaiseAlert(WatchMetricType.PauseDuration, WatchAlertLevel.Info,
                        $"Total GC pause time: {totalPause:F0}ms", totalPause);

                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] Heap: Gen0={gen0 / 1024}KB Gen1={gen1 / 1024}KB Gen2={gen2MB}MB LOH={lohMB}MB Pause={totalPause:F0}ms");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorGcHeap error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    #endregion

    #region UI Thread Responsiveness Monitoring

    private async Task MonitorUiThreadResponsivenessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.UiThreadMonitoringIntervalMs, cancellationToken);

                var check = _uiThreadCheck;
                if (check == null) continue;

                var sw = Stopwatch.StartNew();
                var responded = await check();
                sw.Stop();

                if (!responded)
                {
                    RaiseAlert(WatchMetricType.Cpu, WatchAlertLevel.Warning,
                        $"UI thread unresponsive for >{_config.UiThreadMaxPauseMs}ms (possible hang)",
                        sw.ElapsedMilliseconds, _config.UiThreadMaxPauseMs);
                }
                else if (sw.ElapsedMilliseconds > _config.UiThreadMaxPauseMs / 2)
                {
                    if (_config.EnableVerboseLogging)
                        Console.WriteLine($@"[WatchDog] UI thread response: {sw.ElapsedMilliseconds}ms");
                }
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                // Ignore errors when dispatcher isn't available (early startup)
            }
        }
    }

    #endregion

    #region Network Monitoring

    private async Task MonitorNetworkAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.NetworkMonitoringIntervalMs, cancellationToken);

                var isAvailable = NetworkInterface.GetIsNetworkAvailable();
                if (!isAvailable)
                {
                    RaiseAlert(WatchMetricType.Network, WatchAlertLevel.Warning,
                        "Network not available", 0);
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                using var ping = new Ping();
                var reply = await ping.SendPingAsync(_config.NetworkPingTarget, 3000);

                if (reply.Status != IPStatus.Success)
                    RaiseAlert(WatchMetricType.Network, WatchAlertLevel.Warning,
                        $"Ping to {_config.NetworkPingTarget} failed: {reply.Status}", 0);
                else if (reply.RoundtripTime > 500)
                    RaiseAlert(WatchMetricType.Network, WatchAlertLevel.Info,
                        $"High latency to {_config.NetworkPingTarget}: {reply.RoundtripTime}ms", reply.RoundtripTime);
                else if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] Network: {reply.RoundtripTime}ms to {_config.NetworkPingTarget}");
            }
            catch (OperationCanceledException) { break; }
            catch (PingException)
            {
                // Expected when offline
            }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorNetwork error: {ex.Message}");
                await Task.Delay(10000, cancellationToken);
            }
        }
    }

    #endregion

    #region Disk I/O Monitoring

    private async Task MonitorDiskIoAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();
        _lastDiskSampleTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.DiskMonitoringIntervalMs, cancellationToken);

                process.Refresh();
                var currentRead = process.TotalProcessorTime.Ticks;

                var now = DateTime.UtcNow;
                if (_lastDiskSampleTime == default)
                {
                    _lastDiskSampleTime = now;
                    continue;
                }

                var elapsed = (now - _lastDiskSampleTime).TotalSeconds;
                if (elapsed <= 0) continue;

                _lastDiskSampleTime = now;

                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] Disk I/O sampled (no per-process I/O counters on all platforms)");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (_config.EnableVerboseLogging)
                    Console.WriteLine($@"[WatchDog] MonitorDiskIo error: {ex.Message}");
                await Task.Delay(10000, cancellationToken);
            }
        }
    }

    #endregion

    #region Snapshot Loop

    private async Task SnapshotLoopAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.SnapshotIntervalMs, cancellationToken);
                process.Refresh();

                _snapshotBuffer.TryPeek(out var previous);
                RecordSnapshot(process, previous);
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    private void RecordSnapshot(Process process, WatchSnapshot? previous)
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        var genInfo = memoryInfo.GenerationInfo;

        var gen0 = genInfo.Length > 0 ? genInfo[0].SizeAfterBytes : 0L;
        var gen1 = genInfo.Length > 1 ? genInfo[1].SizeAfterBytes : 0L;
        var gen2 = genInfo.Length > 2 ? genInfo[2].SizeAfterBytes : 0L;
        var lohBytes = memoryInfo.HeapSizeBytes - gen0 - gen1 - gen2;
        if (lohBytes < 0) lohBytes = 0;

        var currentTotalPauseMs = GC.GetTotalPauseDuration().TotalMilliseconds;
        var pauseDeltaMs = currentTotalPauseMs - _lastTotalPauseMs;
        _lastTotalPauseMs = currentTotalPauseMs;

        var snapshot = new WatchSnapshot
        {
            Timestamp = DateTime.Now,
            WorkingSetMB = process.WorkingSet64 / (1024 * 1024),
            PrivateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024),
            ManagedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024),
            CpuUsagePercent = previous?.CpuUsagePercent ?? 0,
            ThreadPoolPendingWorkItems = GetPendingWorkItems(),
            ThreadPoolThreadCount = ThreadPool.ThreadCount,
            ThreadPoolCompletionPortThreads = (int)ThreadPool.CompletedWorkItemCount,
            HandleCount = process.HandleCount,
            Gen0HeapBytes = gen0,
            Gen1HeapBytes = gen1,
            Gen2HeapBytes = gen2,
            LargeObjectHeapBytes = lohBytes,
            FinalizationQueueCount = 0,
            PauseDurationMs = (long)pauseDeltaMs,
            NetworkAvailable = NetworkInterface.GetIsNetworkAvailable(),
            DiskReadMBps = 0,
            DiskWriteMBps = 0
        };

        _snapshotBuffer.Enqueue(snapshot);
        while (_snapshotBuffer.Count > MaxSnapshots)
            _snapshotBuffer.TryDequeue(out _);
    }

    #endregion

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
    }
}
