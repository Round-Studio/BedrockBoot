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
        private readonly string _fileHash;
        private ThemePackManifest _manifest;
        private bool _extracted;
        private bool _disposed;
        private string _cachedIconPath;
        private string _cachedBackgroundPath;
        private string _cachedMusicPath;

        public ThemePackAnalyze(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                throw new ArgumentException("Invalid file path");

            _filePath = file;
            _fileHash = ComputeFileHash(file);

            var hashFolder = Path.Combine(PathsList.TempPath, $"theme_cache_{_fileHash}");
            _tempExtractPath = hashFolder;

            if (IsCacheValid())
            {
                _extracted = true;
                var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
                var jsonContent = File.ReadAllText(manifestPath);
                _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent)
                            ?? throw new InvalidOperationException("Failed to parse manifest.json");
                _manifest.PackHash = _fileHash;
                CacheFilePaths();
            }
            else
            {
                if (Directory.Exists(_tempExtractPath))
                    Directory.Delete(_tempExtractPath, true);
                
                ExtractAllFiles();
                _extracted = true;
                CacheFilePaths();
            }
        }

        private bool IsCacheValid()
        {
            if (!Directory.Exists(_tempExtractPath))
                return false;

            var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
            if (!File.Exists(manifestPath))
                return false;

            try
            {
                var jsonContent = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent);
                if (manifest == null)
                    return false;

                if (!string.IsNullOrEmpty(manifest.PackIconFileName))
                {
                    var iconPath = Path.Combine(_tempExtractPath, manifest.PackIconFileName);
                    if (!File.Exists(iconPath))
                        return false;
                }

                if (!string.IsNullOrEmpty(manifest.BackgroundImageFileName))
                {
                    var bgPath = Path.Combine(_tempExtractPath, "background", manifest.BackgroundImageFileName);
                    if (!File.Exists(bgPath))
                        return false;
                }

                if (!string.IsNullOrEmpty(manifest.BackgroundMusicFileName))
                {
                    var musicPath = Path.Combine(_tempExtractPath, "music", manifest.BackgroundMusicFileName);
                    if (!File.Exists(musicPath))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ExtractAllFiles()
        {
            Directory.CreateDirectory(_tempExtractPath);
            
            using var archive = ZipFile.OpenRead(_filePath);
            
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var targetPath = Path.Combine(_tempExtractPath, entry.FullName);
                var targetDir = Path.GetDirectoryName(targetPath);
                
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
                
                entry.ExtractToFile(targetPath, true);
            }

            var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException("manifest.json not found in zip");

            var jsonContent = File.ReadAllText(manifestPath);
            _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent)
                        ?? throw new InvalidOperationException("Failed to parse manifest.json");
            _manifest.PackHash = _fileHash;
        }

        private void CacheFilePaths()
        {
            if (_manifest == null)
                return;

            if (!string.IsNullOrEmpty(_manifest.PackIconFileName))
            {
                var path = Path.Combine(_tempExtractPath, _manifest.PackIconFileName);
                _cachedIconPath = File.Exists(path) ? path : null;
            }

            if (!string.IsNullOrEmpty(_manifest.BackgroundImageFileName))
            {
                var path = Path.Combine(_tempExtractPath, "background", _manifest.BackgroundImageFileName);
                _cachedBackgroundPath = File.Exists(path) ? path : null;
            }

            if (!string.IsNullOrEmpty(_manifest.BackgroundMusicFileName))
            {
                var path = Path.Combine(_tempExtractPath, "music", _manifest.BackgroundMusicFileName);
                _cachedMusicPath = File.Exists(path) ? path : null;
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

        public string GetPackIconPath()
        {
            return _cachedIconPath;
        }

        public string GetBackgroundImagePath()
        {
            return _cachedBackgroundPath;
        }

        public string GetBackgroundMusicPath()
        {
            return _cachedMusicPath;
        }

        public bool HasPackIcon()
        {
            return _cachedIconPath != null;
        }

        public bool HasBackgroundImage()
        {
            return _cachedBackgroundPath != null;
        }

        public bool HasBackgroundMusic()
        {
            return _cachedMusicPath != null;
        }

        public void Cleanup()
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}