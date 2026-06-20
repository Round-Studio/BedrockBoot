using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BedrockBoot.Base.Entry.Pack.Theme;
using BedrockBoot.Models.Global;
using Round.SDK.Helper;

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
                ZipHelper.ExtractZipFile(file, _tempExtractPath);
                _extracted = true;
            }

            var manifestPath = Path.Combine(_tempExtractPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException("manifest.json not found");

            var jsonContent = File.ReadAllText(manifestPath);
            _manifest = JsonSerializer.Deserialize<ThemePackManifest>(jsonContent)
                        ?? throw new InvalidOperationException("Failed to parse manifest.json");
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

        public byte[] GetPackIconBytes()
        {
            if (string.IsNullOrEmpty(_manifest.PackIconFileName))
                return null;

            using var archive = ZipFile.OpenRead(_filePath);
            var entry = archive.GetEntry(_manifest.PackIconFileName);
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public byte[] GetBackgroundImageBytes()
        {
            if (string.IsNullOrEmpty(_manifest.BackgroundImageFileName))
                return null;

            using var archive = ZipFile.OpenRead(_filePath);
            var entry = archive.GetEntry($"background/{_manifest.BackgroundImageFileName}");
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public byte[] GetBackgroundMusicBytes()
        {
            if (string.IsNullOrEmpty(_manifest.BackgroundMusicFileName))
                return null;

            using var archive = ZipFile.OpenRead(_filePath);
            var entry = archive.GetEntry($"music/{_manifest.BackgroundMusicFileName}");
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public string GetPackIconPath()
        {
            return GetFullPath(_manifest.PackIconFileName);
        }

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

        public bool HasPackIcon()
        {
            return !string.IsNullOrEmpty(_manifest.PackIconFileName);
        }

        public bool HasBackgroundImage()
        {
            return !string.IsNullOrEmpty(_manifest.BackgroundImageFileName);
        }

        public bool HasBackgroundMusic()
        {
            return !string.IsNullOrEmpty(_manifest.BackgroundMusicFileName);
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