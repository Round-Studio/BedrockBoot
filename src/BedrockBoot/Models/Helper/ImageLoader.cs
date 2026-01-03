using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace BedrockBoot.Models.Helper;

public class ImageLoader : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, Bitmap> _imageCache;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public ImageLoader()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _imageCache = new Dictionary<string, Bitmap>();
    }
    
    /// <summary>
    /// 从 URL 加载图片并创建 ImageBrush
    /// </summary>
    public async Task<IImage> LoadImageBrushAsync(string imageUrl, 
        Stretch stretch = Stretch.Uniform, 
        bool useCache = true)
    {
        // 检查缓存
        if (useCache && _imageCache.TryGetValue(imageUrl, out var cachedBitmap))
        {
            return cachedBitmap;
        }
        
        try
        {
            // 限制并发访问
            // await _semaphore.WaitAsync();
            
            // 再次检查缓存（防止重复加载）
            if (useCache && _imageCache.TryGetValue(imageUrl, out cachedBitmap))
            {
                return cachedBitmap;
            }
            
            // 下载图片
            byte[] imageData = await _httpClient.GetByteArrayAsync(imageUrl);
            
            // 在 UI 线程上创建 Bitmap（Bitmap 需要在 UI 线程创建）
            Bitmap bitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using var memoryStream = new MemoryStream(imageData);
                return new Bitmap(memoryStream);
            });
            
            // 缓存图片
            if (useCache)
            {
                _imageCache[imageUrl] = bitmap;
            }
            
            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载图片失败: {imageUrl}, 错误: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 从流创建 ImageBrush
    /// </summary>
    public async Task<IImage> LoadImageBrushFromStreamAsync(Stream stream, 
        Stretch stretch = Stretch.Uniform)
    {
        try
        {
            // 在 UI 线程上创建 Bitmap
            var bitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                return new Bitmap(stream);
            });
            
            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"从流加载图片失败: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 清除指定 URL 的缓存
    /// </summary>
    public void ClearCache(string imageUrl)
    {
        if (_imageCache.ContainsKey(imageUrl))
        {
            _imageCache[imageUrl].Dispose();
            _imageCache.Remove(imageUrl);
        }
    }
    
    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        foreach (var bitmap in _imageCache.Values)
        {
            bitmap.Dispose();
        }
        _imageCache.Clear();
    }
    
    public void Dispose()
    {
        _httpClient.Dispose();
        _semaphore.Dispose();
        ClearAllCache();
    }
}