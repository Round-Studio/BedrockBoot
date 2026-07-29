namespace BedrockBoot.Models.Helper;

public static class FileLock
{
    static readonly Dictionary<string, FileStream> _locks = new();

    public static IDisposable Acquire(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.None);
        try { fs.Lock(0, 0); } catch (PlatformNotSupportedException) { /* fallback for macOS */ }
        _locks[path] = fs;
        try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }
        return new LockDisposable(path);
    }

    class LockDisposable : IDisposable
    {
        readonly string _path;
        public LockDisposable(string path) => _path = path;
        public void Dispose()
        {
            if (_locks.TryGetValue(_path, out var fs))
            {
                try { fs.Unlock(0, 0); } catch (PlatformNotSupportedException) { }
                fs.Dispose();
                _locks.Remove(_path);
            }
        }
    }
}