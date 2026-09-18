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