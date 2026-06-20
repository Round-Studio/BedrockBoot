using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BedrockBoot.Base.Entry.Pack.Theme;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Theme
{
    public class ThemePackAnalyze : IDisposable
    {
        private readonly string _filePath;
        private readonly string _tempExtractPath;
        private readonly ThemePackManifest _manifest;
        private readonly string _fileHash;
        private bool _extracted;
        private bool _disposed;

        public ThemePackAnalyze(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                throw new ArgumentException("Invalid file path");

            _filePath = file;
            _fileHash = ComputeFileHash(file);

            var hashFolder = Path.Combine(PathsList.TempPath, $"theme_cache_{_fileHash}");
            _tempExtractPath = hashFolder;

            if (Directory.Exists(_tempExtractPath) && File.Exists(Path.Combine(_tempExtractPath, "manifest.json")))
            {
                _extracted = true;
            }
            else
            {
                if (Directory.Exists(_tempExtractPath))
                    Directory.Delete(_tempExtractPath, true);
                Directory.CreateDirectory(_tempExtractPath);
                
                using var archive = ZipFile.OpenRead(file);
                
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                    throw new InvalidOperationException("manifest.json not found in zip");
                
                using var stream = manifestEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                using var reader = new StreamReader(ms);
                var jsonContent = reader.ReadToEnd();
                _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent)
                            ?? throw new InvalidOperationException("Failed to parse manifest.json");
                
                manifestEntry.ExtractToFile(Path.Combine(_tempExtractPath, "manifest.json"), true);
                
                if (!string.IsNullOrEmpty(_manifest.PackIconFileName))
                {
                    var iconEntry = archive.GetEntry(_manifest.PackIconFileName);
                    if (iconEntry != null)
                    {
                        var iconPath = Path.Combine(_tempExtractPath, _manifest.PackIconFileName);
                        var iconDir = Path.GetDirectoryName(iconPath);
                        if (!string.IsNullOrEmpty(iconDir) && !Directory.Exists(iconDir))
                            Directory.CreateDirectory(iconDir);
                        iconEntry.ExtractToFile(iconPath, true);
                    }
                }
                
                if (!string.IsNullOrEmpty(_manifest.BackgroundImageFileName))
                {
                    var bgEntry = archive.GetEntry($"background/{_manifest.BackgroundImageFileName}");
                    if (bgEntry != null)
                    {
                        var bgPath = Path.Combine(_tempExtractPath, "background", _manifest.BackgroundImageFileName);
                        var bgDir = Path.GetDirectoryName(bgPath);
                        if (!string.IsNullOrEmpty(bgDir) && !Directory.Exists(bgDir))
                            Directory.CreateDirectory(bgDir);
                        bgEntry.ExtractToFile(bgPath, true);
                    }
                }
                
                if (!string.IsNullOrEmpty(_manifest.BackgroundMusicFileName))
                {
                    var musicEntry = archive.GetEntry($"music/{_manifest.BackgroundMusicFileName}");
                    if (musicEntry != null)
                    {
                        var musicPath = Path.Combine(_tempExtractPath, "music", _manifest.BackgroundMusicFileName);
                        var musicDir = Path.GetDirectoryName(musicPath);
                        if (!string.IsNullOrEmpty(musicDir) && !Directory.Exists(musicDir))
                            Directory.CreateDirectory(musicDir);
                        musicEntry.ExtractToFile(musicPath, true);
                    }
                }
                
                _extracted = true;
            }

            if (_manifest == null)
            {
                var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
                if (!File.Exists(manifestPath))
                    throw new InvalidOperationException("manifest.json not found");
                var jsonContent = File.ReadAllText(manifestPath);
                _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent)
                            ?? throw new InvalidOperationException("Failed to parse manifest.json");
                _manifest.PackHash = _fileHash;
            }
        }

        public ThemePackManifest Manifest => _manifest;

        private string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }

        private string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            var fullPath = Path.Combine(_tempExtractPath, relativePath);
            return File.Exists(fullPath) ? fullPath : null;
        }

        public string GetPackIconPath()
        {
            if (!_extracted || _manifest == null)
                return null;
            return GetFullPath(_manifest.PackIconFileName);
        }

        public string GetBackgroundImagePath()
        {
            if (!_extracted || _manifest == null)
                return null;
            if (string.IsNullOrEmpty(_manifest.BackgroundImageFileName))
                return null;
            return GetFullPath(Path.Combine("background", _manifest.BackgroundImageFileName));
        }

        public string GetBackgroundMusicPath()
        {
            if (!_extracted || _manifest == null)
                return null;
            if (string.IsNullOrEmpty(_manifest.BackgroundMusicFileName))
                return null;
            return GetFullPath(Path.Combine("music", _manifest.BackgroundMusicFileName));
        }

        public bool HasPackIcon()
        {
            return _manifest != null && !string.IsNullOrEmpty(_manifest.PackIconFileName) && GetPackIconPath() != null;
        }

        public bool HasBackgroundImage()
        {
            return _manifest != null && !string.IsNullOrEmpty(_manifest.BackgroundImageFileName) && GetBackgroundImagePath() != null;
        }

        public bool HasBackgroundMusic()
        {
            return _manifest != null && !string.IsNullOrEmpty(_manifest.BackgroundMusicFileName) && GetBackgroundMusicPath() != null;
        }

        public void Cleanup()
        {
            if (Directory.Exists(_tempExtractPath))
                try
                {
                    Directory.Delete(_tempExtractPath, true);
                }
                catch
                {
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
}