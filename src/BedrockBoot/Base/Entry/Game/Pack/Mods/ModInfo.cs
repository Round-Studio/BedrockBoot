using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Platform;

namespace BedrockBoot.Base.Entry.Game.Pack.Mods;

public class ModInfo
{
    /// <summary>
    /// File 为模组文件
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; }

    /// <summary>
    /// InjectDelay 为注入延迟，单位 ms
    /// </summary>
    [JsonPropertyName("injectDelay")]
    public int InjectDelay { get; set; } = 0;

    /// <summary>
    /// IsPreLoad 为是否启用预加载
    /// </summary>
    [JsonPropertyName("isPreLoad")]
    public bool IsPreLoad { get; set; } = false;

    public void Inject(int processId)
    {
        if(IsPreLoad)
            throw new Exception("This mod is PreLoad mod.");
        
        var assembly = Assembly.GetExecutingAssembly();

        string resourceName = "BedrockBoot.Assets.PreloadCpp.dll";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    BedrockBoot.Inject.Native.Init(memoryStream.ToArray());
                    BedrockBoot.Inject.Native.LoadPlugins(processId, Path.GetFullPath(File), InjectDelay != 0, InjectDelay);
                }
            }
        }
    }

    private byte[] GetAssetBytes(string uri)
    {
        try
        {
            // 确保URI格式正确
            if (!uri.StartsWith("avares://"))
            {
                uri = $"avares://{uri.TrimStart('/')}";
            }

            // 使用AssetLoader的静态方法
            var uriObj = new Uri(uri);

            // 检查资源是否存在
            if (!AssetLoader.Exists(uriObj))
            {
                throw new FileNotFoundException($"Asset not found: {uri}");
            }

            // 打开资源流
            using (var stream = AssetLoader.Open(uriObj))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to load asset from URI: {uri}", ex);
        }
    }
}