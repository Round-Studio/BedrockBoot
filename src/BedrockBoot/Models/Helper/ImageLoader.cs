using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Core.Global;
using GlobalModel = BedrockBoot.Models.Global.GlobalModel;

namespace BedrockBoot.Models.Helper;

public class ImageLoader : IDisposable
{
    private readonly HttpClient _httpClient;

    // LRU 内存缓存（按访问顺序，最近访问的排到队首）
    private readonly LinkedList<CacheEntry> _lruList = new();
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _lruIndex = new(StringComparer.Ordinal);
    private readonly object _lruLock = new();

    // 每个 URL 一个信号量，保证同一张图不会并发下载，但不同图可并行
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _urlLocks = new(StringComparer.Ordinal);

    // 缓存总像素上限（默认 1.5 亿像素 ≈ 100 张 1280x720），超出后按 LRU 淘汰
    private const long MaxCachePixels = 150_000_000L;
    private long _currentPixels;

    private readonly string _localCacheFolder;
    private bool _disposed;

    public ImageLoader()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _localCacheFolder = PathsList.TempPath;
        if (!Directory.Exists(_localCacheFolder)) Directory.CreateDirectory(_localCacheFolder);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
        ClearMemoryCache();
        foreach (var sem in _urlLocks.Values) sem.Dispose();
        _urlLocks.Clear();
        GC.SuppressFinalize(this);
    }
    
    public async Task<byte[]> BitmapTaskToByteArrayAsync(Task<Bitmap?> bitmapTask)
    {
        // 等待 Task 完成并获取 Bitmap
        Bitmap? bitmap = await bitmapTask;
    
        if (bitmap == null)
            return Array.Empty<byte>();
    
        // 使用 MemoryStream 保存编码后的数据
        using var memoryStream = new MemoryStream();
    
        // 编码为 PNG 格式
        bitmap.Save(memoryStream);
    
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     从 URL 加载图片（内存 -> 磁盘 -> 网络）。同一 URL 并发只会下载一次。
    /// </summary>
    public async Task<Bitmap?> LoadImageBrushAsync(string imageUrl, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        if (useCache && TryGetFromCache(imageUrl, out var cached)) return cached;

        var urlLock = _urlLocks.GetOrAdd(imageUrl, _ => new SemaphoreSlim(1, 1));
        await urlLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (useCache && TryGetFromCache(imageUrl, out cached)) return cached;

            byte[]? imageData = null;
            var localPath = GetLocalFilePath(imageUrl);

            if (useCache && File.Exists(localPath))
                try
                {
                    imageData = await File.ReadAllBytesAsync(localPath).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"读取磁盘缓存失败: {ex.Message}");
                }

            if (imageData == null)
            {
                imageData = await _httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
                if (useCache && imageData != null)
                {
                    try { _ = File.WriteAllBytesAsync(localPath, imageData); }
                    catch { /* 忽略磁盘写入失败 */ }
                }
            }

            if (imageData == null) return null;

            var bitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using var ms = new MemoryStream(imageData);
                return new Bitmap(ms);
            });

            if (useCache && bitmap != null) AddToCache(imageUrl, bitmap);

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载图片失败: {imageUrl}, 错误: {ex.Message}");
            return null;
        }
        finally
        {
            urlLock.Release();
        }
    }

    /// <summary>
    ///     从流创建图片
    /// </summary>
    public async Task<Bitmap?> LoadImageBrushFromStreamAsync(Stream stream)
    {
        try
        {
            return await Dispatcher.UIThread.InvokeAsync(() => new Bitmap(stream));
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"从流加载图片失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     根据 URL 生成唯一的本地文件名（SHA1）
    /// </summary>
    private string GetLocalFilePath(string url)
    {
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(url));
        var fileName = Convert.ToHexString(hashBytes).ToLower();
        return Path.Combine(_localCacheFolder, fileName);
    }

    private bool TryGetFromCache(string key, out Bitmap? bitmap)
    {
        lock (_lruLock)
        {
            if (_lruIndex.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                bitmap = node.Value.Bitmap;
                return bitmap != null;
            }
        }
        bitmap = null;
        return false;
    }

    private void AddToCache(string key, Bitmap bitmap)
    {
        var pixels = (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height;
        if (pixels <= 0) return;

        lock (_lruLock)
        {
            if (_lruIndex.TryGetValue(key, out var existing))
            {
                _currentPixels -= existing.Value.PixelCount;
                existing.Value.Dispose();
                _lruList.Remove(existing);
                _lruIndex.Remove(key);
            }

            var entry = new CacheEntry(key, bitmap, pixels);
            var node = new LinkedListNode<CacheEntry>(entry);
            _lruList.AddFirst(node);
            _lruIndex[key] = node;
            _currentPixels += pixels;

            // 超限则从最久未使用的一端淘汰
            while (_currentPixels > MaxCachePixels && _lruList.Count > 1)
            {
                var last = _lruList.Last;
                if (last == null) break;
                _currentPixels -= last.Value.PixelCount;
                _lruIndex.Remove(last.Value.Key);
                last.Value.Dispose();
                _lruList.RemoveLast();
            }
        }
    }

    /// <summary>
    ///     清除内存缓存
    /// </summary>
    public void ClearMemoryCache()
    {
        lock (_lruLock)
        {
            foreach (var entry in _lruList) entry.Dispose();
            _lruList.Clear();
            _lruIndex.Clear();
            _currentPixels = 0;
        }
    }

    /// <summary>
    ///     清除磁盘上的所有图片缓存
    /// </summary>
    public void ClearDiskCache()
    {
        try
        {
            if (Directory.Exists(_localCacheFolder))
            {
                Directory.Delete(_localCacheFolder, true);
                Directory.CreateDirectory(_localCacheFolder);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"清理磁盘缓存失败: {ex.Message}");
        }
    }

    public static async Task<Bitmap?> LoadIconAsync(string iconUri)
    {
        if (iconUri.StartsWith("avares://")) return new Bitmap(AssetLoader.Open(new Uri(iconUri)));

        if (iconUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return await GlobalModel.ImageLoader.LoadImageBrushAsync(iconUri);

        if (File.Exists(iconUri))
            return new Bitmap(iconUri);
        return null;
    }

    private sealed class CacheEntry
    {
        public CacheEntry(string key, Bitmap bitmap, long pixelCount)
        {
            Key = key;
            Bitmap = bitmap;
            PixelCount = pixelCount;
        }

        public string Key { get; }
        public Bitmap Bitmap { get; }
        public long PixelCount { get; }

        public void Dispose() => Bitmap.Dispose();
    }
}
