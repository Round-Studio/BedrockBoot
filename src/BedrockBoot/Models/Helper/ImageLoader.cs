using System;
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
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, Bitmap> _imageCache;

    // 缓存根目录：优先使用 AppData，无权限则使用程序根目录
    private readonly string _localCacheFolder;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ImageLoader()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _imageCache = new Dictionary<string, Bitmap>();

        _localCacheFolder = PathsList.TempPath;

        if (!Directory.Exists(_localCacheFolder)) Directory.CreateDirectory(_localCacheFolder);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _semaphore.Dispose();
        ClearMemoryCache();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     从 URL 加载图片（内存 -> 磁盘 -> 网络）
    /// </summary>
    public async Task<Bitmap?> LoadImageBrushAsync(string imageUrl, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        // 1. 内存缓存检查
        if (useCache && _imageCache.TryGetValue(imageUrl, out var cachedBitmap)) return cachedBitmap;

        // 使用信号量防止并发请求同一个 URL 时多次下载
        await _semaphore.WaitAsync();
        try
        {
            // 二次检查内存（双重锁定检查）
            if (useCache && _imageCache.TryGetValue(imageUrl, out cachedBitmap)) return cachedBitmap;

            var localPath = GetLocalFilePath(imageUrl);
            byte[]? imageData = null;

            // 2. 磁盘缓存检查
            if (useCache && File.Exists(localPath))
                try
                {
                    imageData = await File.ReadAllBytesAsync(localPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"读取磁盘缓存失败: {ex.Message}");
                }

            // 3. 网络下载
            if (imageData == null)
            {
                imageData = await _httpClient.GetByteArrayAsync(imageUrl);

                // 写入磁盘异步进行
                if (useCache && imageData != null) _ = File.WriteAllBytesAsync(localPath, imageData);
            }

            if (imageData == null) return null;

            // 4. 在 UI 线程创建 Bitmap
            var bitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using var memoryStream = new MemoryStream(imageData);
                return new Bitmap(memoryStream);
            });

            // 更新内存缓存
            if (useCache && bitmap != null) _imageCache[imageUrl] = bitmap;

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载图片失败: {imageUrl}, 错误: {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
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

    /// <summary>
    ///     清除内存缓存
    /// </summary>
    public void ClearMemoryCache()
    {
        foreach (var bitmap in _imageCache.Values) bitmap.Dispose();

        _imageCache.Clear();
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
        return null;
    }
}