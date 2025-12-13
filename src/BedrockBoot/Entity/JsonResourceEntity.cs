using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace BedrockBoot.Entity;

public class JsonResourceEntity
{
    public async Task<string> ReadJsonResourceAsync(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
    
    public async Task<T> LoadJsonResourceAsync<T>(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }
}