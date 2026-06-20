using System;
using System.IO;
using System.Text.Json;
using BedrockBoot.Base.Entry.Pack.Theme;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Global;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Theme;

public class ThemePackAnalyze : IDisposable
{
    private readonly string _tempExtractPath;
    private readonly ThemePackManifest _manifest;
    private bool _disposed;

    public ThemePackAnalyze(string file)
    {
        if (string.IsNullOrEmpty(file) || !File.Exists(file))
            throw new ArgumentException("Invalid file path");

        _tempExtractPath = Path.Combine(PathsList.TempPath, $"theme_extract_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempExtractPath);
        
        ZipHelper.ExtractZipFile(file, _tempExtractPath);
        
        var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException("manifest.json not found");
            
        var jsonContent = File.ReadAllText(manifestPath);
        _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent) 
            ?? throw new InvalidOperationException("Failed to parse manifest.json");
    }

    public ThemePackManifest Manifest => _manifest;

    private string GetFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        var fullPath = Path.Combine(_tempExtractPath, relativePath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    public string GetPackIconPath() => GetFullPath(_manifest.PackIconFileName);

    public string GetBackgroundImagePath()
    {
        if (string.IsNullOrEmpty(_manifest.BackgroundImageFileName))
            return null;
        return GetFullPath(Path.Combine("background", _manifest.BackgroundImageFileName));
    }

    public string GetBackgroundMusicPath()
    {
        if (string.IsNullOrEmpty(_manifest.BackgroundMusicFileName))
            return null;
        return GetFullPath(Path.Combine("music", _manifest.BackgroundMusicFileName));
    }

    public bool HasPackIcon() => !string.IsNullOrEmpty(_manifest.PackIconFileName) && GetPackIconPath() != null;

    public bool HasBackgroundImage() => !string.IsNullOrEmpty(_manifest.BackgroundImageFileName) && GetBackgroundImagePath() != null;

    public bool HasBackgroundMusic() => !string.IsNullOrEmpty(_manifest.BackgroundMusicFileName) && GetBackgroundMusicPath() != null;

    public void Cleanup()
    {
        if (Directory.Exists(_tempExtractPath))
        {
            try
            {
                Directory.Delete(_tempExtractPath, true);
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}