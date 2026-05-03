using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockRender;
using BedrockRender.Avalonia;
using BedrockWorld;
using BedrockWorld.Chunk;

namespace BedrockBoot.Views.Windows.SubWindows;

public partial class LevelRenderWindow : Window
{
    private sealed record LoadedWorldState(
        StreamingWorld StreamingWorld,
        StreamingMapRenderer StreamingRenderer,
        ChunkBounds Bounds);

    private readonly record struct ChunkBounds(int MinX, int MinZ, int MaxX, int MaxZ, int Count)
    {
        public bool HasChunks => Count > 0;
    }

    private StreamingWorld? _streamingWorld;
    private StreamingMapRenderer? _streamingRenderer;
    private List<string> _recentFolders = new();
    private const int MaxRecentFolders = 10;

    private CancellationTokenSource? _renderCancellation;
    private bool _isRendering;

    private int _minChunkX = -64;
    private int _minChunkZ = -64;
    private int _maxChunkX = 64;
    private int _maxChunkZ = 64;
    private string? _currentWorldPath;

    private RenderMode _currentRenderMode = RenderMode.SurfaceBlocks;
    private int _currentLayerY = 64;
    private Dimension _currentDimension = Dimension.Overworld;

    private DispatcherTimer? _renderThrottleTimer;
    private bool _pendingRenderRequest;
    private int _renderGeneration;
    private int _imageBegunForCurrentRender;
    private int _pendingImageWidth;
    private int _pendingImageHeight;
    private int _pendingOriginWorldX;
    private int _pendingOriginWorldZ;
    private bool _preserveCurrentImageUntilNextRender;
    
    public LevelRenderWindow()
    {
        InitializeComponent();
    }
    
    public LevelRenderWindow(string currentWorldPath):this()
    {
        LoadWorld(currentWorldPath);
    }
    
    private async Task LoadWorld(string folderPath)
    {
        LoadedWorldState? loadedState = null;

        try
        {
            MapRenderView.ShowIndeterminateProgress("正在读取存档...");

            _renderCancellation?.Cancel();
            _streamingRenderer?.Dispose();
            _streamingWorld?.Dispose();
            MapRenderView.ClearImage(returnPixelBuffer: true);

            var dimension = _currentDimension;
            loadedState = await Task.Run(() =>
            {
                var streamingWorld = new StreamingWorld(folderPath);

                var palette = AvaloniaRenderPalette.LoadDefault();
                var streamingRenderer = new StreamingMapRenderer(streamingWorld, palette);
                var bounds = ScanChunkBounds(streamingWorld, dimension);

                return new LoadedWorldState(streamingWorld, streamingRenderer, bounds);
            });

            _streamingWorld = loadedState.StreamingWorld;
            _streamingRenderer = loadedState.StreamingRenderer;
            var bounds = loadedState.Bounds;
            loadedState = null;
            _currentWorldPath = folderPath;
            if (bounds.HasChunks)
            {
                _minChunkX = bounds.MinX;
                _minChunkZ = bounds.MinZ;
                _maxChunkX = bounds.MaxX;
                _maxChunkZ = bounds.MaxZ;
                var width = ((long)_maxChunkX - _minChunkX + 1) * 16;
                var height = ((long)_maxChunkZ - _minChunkZ + 1) * 16;
            }
            await StartStreamingRender();
        }
        catch (Exception ex)
        {
            MapRenderView.HideProgress();
            loadedState?.StreamingWorld.Dispose();
            loadedState?.StreamingRenderer.Dispose();
        }
    }
    

    private static ChunkBounds ScanChunkBounds(StreamingWorld streamingWorld, Dimension dimension)
    {
        var minX = int.MaxValue;
        var minZ = int.MaxValue;
        var maxX = int.MinValue;
        var maxZ = int.MinValue;
        var count = 0;
        var seen = new HashSet<ChunkPos>();

        foreach (var chunk in streamingWorld.EnumerateChunkPositions(dimension))
        {
            if (!seen.Add(chunk))
                continue;

            count++;
            minX = Math.Min(minX, chunk.X);
            minZ = Math.Min(minZ, chunk.Z);
            maxX = Math.Max(maxX, chunk.X);
            maxZ = Math.Max(maxZ, chunk.Z);
        }

        return count == 0
            ? new ChunkBounds(0, 0, 0, 0, 0)
            : new ChunkBounds(minX, minZ, maxX, maxZ, count);
    }
    

    private async Task StartStreamingRender()
    {
        if (_streamingRenderer == null || _streamingWorld == null)
            return;

        if (_isRendering)
        {
            _renderCancellation?.Cancel();
            _streamingRenderer.CancelCurrentRender();
            _pendingRenderRequest = true;
            return;
        }

        _isRendering = true;
        _pendingRenderRequest = false;
        var renderGeneration = Interlocked.Increment(ref _renderGeneration);
        _renderCancellation = new CancellationTokenSource();
        var token = _renderCancellation.Token;

        try
        {
            // 预计算尺寸
            var widthLong = ((long)_maxChunkX - _minChunkX + 1) * 16;
            var heightLong = ((long)_maxChunkZ - _minChunkZ + 1) * 16;
            var pixelCount = widthLong * heightLong;

            // 限制最大分配，防止因为地图过大直接分配几个 G 的数组
            if (pixelCount > 512 * 1024 * 1024) // 限制为 512M 像素 (约 2GB RAM)
            {
                return;
            }

            _pendingImageWidth = (int)widthLong;
            _pendingImageHeight = (int)heightLong;
            _pendingOriginWorldX = _minChunkX * 16;
            _pendingOriginWorldZ = _minChunkZ * 16;
            if (_preserveCurrentImageUntilNextRender)
            {
                Volatile.Write(ref _imageBegunForCurrentRender, 0);
            }
            else
            {
                MapRenderView.BeginImage(_pendingImageWidth, _pendingImageHeight, _pendingOriginWorldX, _pendingOriginWorldZ);
                MapRenderView.ResetView();
                Volatile.Write(ref _imageBegunForCurrentRender, 1);
            }
            MapRenderView.ShowProgress(0, "准备渲染...");

            // 2. 挂载事件
            _streamingRenderer.ProgressChanged += OnRenderProgressChanged;
            _streamingRenderer.ChunkRendered += OnChunkRendered;

            // 3. 执行异步渲染
            await _streamingRenderer.RenderChunksProgressiveAsync(
                _currentDimension,
                _minChunkX, _minChunkZ, _maxChunkX, _maxChunkZ,
                null,
                -64, 320,
                _currentLayerY,
                _currentRenderMode,
                token);

            if (token.IsCancellationRequested)
            {
                if (!_pendingRenderRequest && renderGeneration == _renderGeneration)
                {
                    MapRenderView.HideProgress();
                }
            }
            else
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(600);
                    if (!_pendingRenderRequest && renderGeneration == _renderGeneration)
                    {
                        MapRenderView.HideProgress();
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            if (!_pendingRenderRequest && renderGeneration == _renderGeneration)
            {
                MapRenderView.HideProgress();
            }
        }
        catch (Exception ex)
        {
            if (renderGeneration == _renderGeneration)
            {
                MapRenderView.HideProgress();
            }
        }
        finally
        {
            // 4. 重要：卸载事件，防止内存泄漏和重复调用
            if (_streamingRenderer != null)
            {
                _streamingRenderer.ProgressChanged -= OnRenderProgressChanged;
                _streamingRenderer.ChunkRendered -= OnChunkRendered;
            }

            _isRendering = false;

            // 如果渲染中途有新的请求，再次触发
            if (_pendingRenderRequest)
            {
                _pendingRenderRequest = false;
                await StartStreamingRender();
            }
        }
    }
    
    

    private void OnRenderProgressChanged(RenderProgress progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            MapRenderView.ShowProgress(
                progress.ProgressPercent,
                $"渲染中... {progress.RenderedChunks}/{progress.TotalChunks} ({progress.ProgressPercent:F1}%)");
        });
    }

    private void OnChunkRendered(ChunkRenderResult result)
    {
        // 关键修复：使用 using 确保在方法结束时执行 result.Dispose() 从而归还 ArrayPool
        using (result)
        {
            try
            {
                EnsureImageBegunForCurrentRender();
                MapRenderView.UpdateChunk(result);
            }
            catch (Exception ex)
            {
                // 捕获异步流水线中的异常，避免因单个 Chunk 错误导致整个程序崩溃
                System.Diagnostics.Debug.WriteLine($"[Render Error] Chunk {result.Position}: {ex.Message}");
            }
        } // 此处 result 自动销毁，内部数组通过 ArrayPool.Return 回收
    }

    private void EnsureImageBegunForCurrentRender()
    {
        if (Interlocked.CompareExchange(ref _imageBegunForCurrentRender, 1, 0) != 0)
            return;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            MapRenderView.BeginImage(_pendingImageWidth, _pendingImageHeight, _pendingOriginWorldX, _pendingOriginWorldZ);
            MapRenderView.ResetView();
        }).GetAwaiter().GetResult();
    }
}