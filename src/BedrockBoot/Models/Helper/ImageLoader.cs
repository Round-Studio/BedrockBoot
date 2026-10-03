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
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Helper;

public class ImageLoader : IDisposable
{
    private static ImageLoader? _shared;

    public static ImageLoader Shared => _shared ??= new ImageLoader();

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
        // 不释放 _urlLocks 里的信号量：可能还有正在进行的加载持有它们，
        // 释放后再 Release/WaitAsync 会抛 ObjectDisposedException
        _urlLocks.Clear();
        GC.SuppressFinalize(this);
    }

    public async Task<Bitmap?> LoadIconAsync(string iconUri, int? decodeWidth = null)
    {
        Console.WriteLine($@"获取图片：{iconUri}");
        if (string.IsNullOrEmpty(iconUri))
            return await LoadIconAsync("avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png");

        if (iconUri.StartsWith("avares://")) return new Bitmap(AssetLoader.Open(new Uri(iconUri)));

        if (iconUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return await LoadImageBrushAsync(iconUri, true, decodeWidth);

        string decodedPath = Uri.UnescapeDataString(iconUri);

        if (File.Exists(decodedPath))
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = File.OpenRead(decodedPath);
                    return decodeWidth is > 0
                        ? Bitmap.DecodeToWidth(stream, decodeWidth.Value)
                        : new Bitmap(stream);
                }
                catch
                {
                    return null;
                }
            });

        return await LoadIconAsync("avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png");
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
    /// <param name="imageUrl">图片地址</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <param name="decodeWidth">按指定宽度解码（用于列表小图标，避免按原图全尺寸解码占用大量内存）</param>
    public async Task<Bitmap?> LoadImageBrushAsync(string imageUrl, bool useCache = true, int? decodeWidth = null)
    {
        if (imageUrl.StartsWith("avares://")) return await LoadIconAsync(imageUrl);

        // 部分在线资源返回的地址会带上多余的逗号/空白，先清理掉
        imageUrl = imageUrl.Trim().TrimEnd(',');

        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        if (_disposed) return null;

        var cacheKey = decodeWidth is > 0 ? $"{imageUrl}#w{decodeWidth.Value}" : imageUrl;

        if (useCache && TryGetFromCache(cacheKey, out var cached)) return cached;

        var urlLock = _urlLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await urlLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (useCache && TryGetFromCache(cacheKey, out cached)) return cached;

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
                    try
                    {
                        _ = File.WriteAllBytesAsync(localPath, imageData);
                    }
                    catch
                    {
                        /* 忽略磁盘写入失败 */
                    }
                }
            }

            if (imageData == null) return null;

            // 在后台线程解码：解码是 CPU 密集操作，放在 UI 线程会阻塞渲染，导致加载圈动画卡顿
            var bitmap = await Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream(imageData);
                    return decodeWidth is > 0
                        ? Bitmap.DecodeToWidth(ms, decodeWidth.Value)
                        : new Bitmap(ms);
                }
                catch
                {
                    return null;
                }
            });

            if (bitmap == null)
                return await LoadIconAsync("avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png");

            if (useCache) AddToCache(cacheKey, bitmap);

            return bitmap;
        }
        catch (Exception ex) when (ex is OperationCanceledException || _disposed)
        {
            // 控件卸载 / 加载器释放导致的取消属于正常情况，静默返回，不打印也不回退占位图
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载图片失败: {imageUrl}, 错误: {ex.Message}");
            return await LoadIconAsync("avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png");
            ;
        }
        finally
        {
            // 加载过程中控件可能已卸载并 Dispose，此时信号量可能已被释放，忽略即可
            try
            {
                urlLock.Release();
            }
            catch (ObjectDisposedException)
            {
            }
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