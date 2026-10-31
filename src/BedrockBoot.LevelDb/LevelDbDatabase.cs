using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BedrockBoot.LevelDb;

public sealed class LevelDbFileInfo
{
    public int Level { get; init; }
    public ulong Number { get; init; }
    public ulong Size { get; init; }
}

public sealed class LevelDbManifest
{
    public string ManifestPath { get; set; } = string.Empty;
    public string? Comparator { get; set; }
    public ulong LogNumber { get; set; }
    public ulong PrevLogNumber { get; set; }
    public ulong NextFileNumber { get; set; }
    public ulong LastSequence { get; set; }
    public List<LevelDbFileInfo> Files { get; } = new();
    public HashSet<(int Level, ulong Number)> DeletedFiles { get; } = new();

    public List<LevelDbFileInfo> GetLiveFiles()
    {
        return Files.Where(f => !DeletedFiles.Contains((f.Level, f.Number))).ToList();
    }
}

// 纯 C# 的 LevelDB 只读实现，兼容 Minecraft 基岩版（leveldb-mcpe）的 zlib 压缩块
public sealed class LevelDbDatabase
{
    public const int BlockSize = 32768;
    public const int LogHeaderSize = 7;
    private const int FooterSize = 48;
    private const ulong TableMagic = 0xdb4775248b80fb57UL;

    private readonly string _dbDir;

    public LevelDbManifest Manifest { get; }

    public LevelDbDatabase(string dbDir)
    {
        _dbDir = dbDir;
        Manifest = ReadManifest();
    }

    public static string ReadCurrentManifestPath(string dbDir)
    {
        var name = File.ReadAllText(Path.Combine(dbDir, "CURRENT")).Trim();
        if (!name.StartsWith("MANIFEST-")) throw new InvalidDataException($@"CURRENT 内容无效: {name}");
        return Path.Combine(dbDir, name);
    }

    private LevelDbManifest ReadManifest()
    {
        var manifest = new LevelDbManifest();
        manifest.ManifestPath = ReadCurrentManifestPath(_dbDir);
        ReadLogRecords(manifest.ManifestPath, edit => ApplyVersionEdit(edit, manifest));
        return manifest;
    }

    private static void ApplyVersionEdit(byte[] data, LevelDbManifest manifest)
    {
        var pos = 0;
        while (pos < data.Length)
        {
            if (!TryReadVarint(data, ref pos, out var tag)) return;
            switch (tag)
            {
                case 1:
                    if (!TryReadLengthPrefixed(data, ref pos, out var comparator)) return;
                    manifest.Comparator = Encoding.UTF8.GetString(comparator);
                    break;
                case 2:
                    if (!TryReadVarint(data, ref pos, out var logNumber)) return;
                    manifest.LogNumber = logNumber;
                    break;
                case 3:
                    if (!TryReadVarint(data, ref pos, out var nextFile)) return;
                    manifest.NextFileNumber = nextFile;
                    break;
                case 4:
                    if (!TryReadVarint(data, ref pos, out var lastSeq)) return;
                    manifest.LastSequence = lastSeq;
                    break;
                case 5:
                    if (!TryReadVarint(data, ref pos, out _)) return;
                    if (!TryReadLengthPrefixed(data, ref pos, out _)) return;
                    break;
                case 6:
                    if (!TryReadVarint(data, ref pos, out var delLevel)) return;
                    if (!TryReadVarint(data, ref pos, out var delNumber)) return;
                    manifest.DeletedFiles.Add(((int)delLevel, delNumber));
                    break;
                case 7:
                    if (!TryReadVarint(data, ref pos, out var level)) return;
                    if (!TryReadVarint(data, ref pos, out var number)) return;
                    if (!TryReadVarint(data, ref pos, out var size)) return;
                    if (!TryReadLengthPrefixed(data, ref pos, out _)) return;
                    if (!TryReadLengthPrefixed(data, ref pos, out _)) return;
                    manifest.Files.Add(new LevelDbFileInfo { Level = (int)level, Number = number, Size = size });
                    break;
                case 9:
                    if (!TryReadVarint(data, ref pos, out var prevLog)) return;
                    manifest.PrevLogNumber = prevLog;
                    break;
                default:
                    return;
            }
        }
    }

    public string GetTablePath(ulong number)
    {
        var baseName = number.ToString("D6");
        var ldb = Path.Combine(_dbDir, baseName + ".ldb");
        return File.Exists(ldb) ? ldb : Path.Combine(_dbDir, baseName + ".sst");
    }

    public IEnumerable<string> GetLogPaths()
    {
        if (!Directory.Exists(_dbDir)) yield break;
        foreach (var path in Directory.GetFiles(_dbDir, "*.log"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!ulong.TryParse(name, out var number)) continue;
            if (number < Manifest.LogNumber) continue;
            yield return path;
        }
    }

    // 按新旧合并返回所有以 prefix 开头的键（同键后写覆盖先写，墓碑即删除）
    public Dictionary<string, byte[]> ScanPrefix(string prefix)
    {
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var state = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        var live = Manifest.GetLiveFiles();
        for (var level = 6; level >= 1; level--)
        {
            foreach (var file in live.Where(f => f.Level == level).OrderBy(f => f.Number))
                ScanTable(GetTablePath(file.Number), prefixBytes, state);
        }

        foreach (var file in live.Where(f => f.Level == 0).OrderBy(f => f.Number))
            ScanTable(GetTablePath(file.Number), prefixBytes, state);

        foreach (var logPath in GetLogPaths().OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.Ordinal))
            ScanLogFile(logPath, prefixBytes, state);

        return state;
    }

    public ulong GetMaxLogSequence()
    {
        ulong max = 0;
        foreach (var logPath in GetLogPaths())
        {
            ReadLogRecords(logPath, record =>
            {
                if (record.Length < 12) return;
                var seq = BinaryPrimitives.ReadUInt64LittleEndian(record);
                var count = BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8));
                if (count > 0 && seq + count - 1 > max) max = seq + count - 1;
            });
        }
        return max;
    }

    private void ScanTable(string path, byte[] prefix, Dictionary<string, byte[]> state)
    {
        if (!File.Exists(path)) return;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < FooterSize) return;

            Span<byte> footer = stackalloc byte[FooterSize];
            fs.Position = fs.Length - FooterSize;
            ReadFully(fs, footer);

            if (BinaryPrimitives.ReadUInt64LittleEndian(footer.Slice(FooterSize - 8)) != TableMagic) return;

            var pos = 0;
            if (!TryReadBlockHandle(footer, ref pos, out _, out _)) return;
            if (!TryReadBlockHandle(footer, ref pos, out var indexOffset, out var indexSize)) return;

            var indexData = ReadBlockData(fs, indexOffset, indexSize);
            if (indexData == null) return;

            var entries = ParseBlockEntries(indexData);
            var start = entries.FindIndex(e => e.Key.AsSpan().SequenceCompareTo(prefix) >= 0);
            if (start < 0) return;

            for (var i = start; i < entries.Count; i++)
            {
                var handle = entries[i];
                var blockPos = 0;
                if (!TryReadBlockHandle(handle.Value, ref blockPos, out var offset, out var size)) continue;

                var blockData = ReadBlockData(fs, offset, size);
                if (blockData == null) continue;

                foreach (var entry in ParseBlockEntries(blockData))
                {
                    var ikey = entry.Key;
                    if (!ikey.AsSpan().StartsWith(prefix)) continue;
                    if (ikey.Length < prefix.Length + 8) continue;

                    var suffix = Encoding.UTF8.GetString(ikey, prefix.Length, ikey.Length - prefix.Length - 8);
                    if (ikey[^1] == 1)
                        state[suffix] = entry.Value;
                    else
                        state.Remove(suffix);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取 SSTable 失败 {path}: {ex.Message}");
        }
    }

    private void ScanLogFile(string path, byte[] prefix, Dictionary<string, byte[]> state)
    {
        try
        {
            ReadLogRecords(path, record => DecodeWriteBatch(record, prefix, state));
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取日志失败 {path}: {ex.Message}");
        }
    }

    private static void DecodeWriteBatch(byte[] record, byte[] prefix, Dictionary<string, byte[]> state)
    {
        if (record.Length < 12) return;
        var pos = 12;
        while (pos < record.Length)
        {
            var opType = record[pos++];
            if (!TryReadVarint(record, ref pos, out var keyLen)) return;
            if (pos + (int)keyLen > record.Length) return;
            var key = record.AsSpan(pos, (int)keyLen).ToArray();
            pos += (int)keyLen;

            var matched = key.AsSpan().StartsWith(prefix);
            byte[]? value = null;

            if (opType == 1)
            {
                if (!TryReadVarint(record, ref pos, out var valueLen)) return;
                if (pos + (int)valueLen > record.Length) return;
                if (matched) value = record.AsSpan(pos, (int)valueLen).ToArray();
                pos += (int)valueLen;
            }

            if (!matched) continue;

            var suffix = Encoding.UTF8.GetString(key, prefix.Length, key.Length - prefix.Length);
            if (opType == 1)
                state[suffix] = value!;
            else
                state.Remove(suffix);
        }
    }

    // 解析 leveldb 日志/MANIFEST 的分块记录格式（32KB 块 + CRC 校验）
    public static void ReadLogRecords(string path, Action<byte[]> record)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Span<byte> header = stackalloc byte[LogHeaderSize];
        using var frag = new MemoryStream();
        var inFragment = false;
        long filePos = 0;
        int blockPos = 0;

        while (true)
        {
            if (blockPos > BlockSize - LogHeaderSize)
            {
                filePos += BlockSize - blockPos;
                blockPos = 0;
            }

            fs.Position = filePos;
            var read = ReadFully(fs, header);
            if (read == 0) return;
            if (read < LogHeaderSize) return;

            var length = header[4] | (header[5] << 8);
            var type = header[6];

            if (length == 0 && type == 0 &&
                header[0] == 0 && header[1] == 0 && header[2] == 0 && header[3] == 0)
                return;

            if (length > BlockSize - blockPos - LogHeaderSize) return;

            var payload = new byte[length];
            if (ReadFully(fs, payload) < length) return;

            var storedCrc = BinaryPrimitives.ReadUInt32LittleEndian(header);
            var crc = Crc32C.Extend(Crc32C.Value([(byte)type]), payload);
            if (Crc32C.Mask(crc) != storedCrc) return;

            switch (type)
            {
                case 1:
                    record(payload);
                    inFragment = false;
                    break;
                case 2:
                    frag.SetLength(0);
                    frag.Write(payload);
                    inFragment = true;
                    break;
                case 3:
                    if (inFragment) frag.Write(payload);
                    break;
                case 4:
                    if (inFragment)
                    {
                        frag.Write(payload);
                        record(frag.ToArray());
                        frag.SetLength(0);
                        inFragment = false;
                    }
                    break;
            }

            filePos += LogHeaderSize + length;
            blockPos += LogHeaderSize + length;
        }
    }

    // 读取并解压一个数据块，返回 null 表示无法解压
    private static byte[]? ReadBlockData(FileStream fs, ulong offset, ulong size)
    {
        try
        {
            var raw = new byte[size + 5];
            fs.Position = (long)offset;
            if (ReadFully(fs, raw) < raw.Length) return null;

            var compressionType = raw[size];
            var payload = raw.AsSpan(0, (int)size).ToArray();

            if (compressionType == 0) return payload;
            if (compressionType == 2) return Decompress(new ZLibStream(new MemoryStream(payload), CompressionMode.Decompress));
            if (compressionType == 4) return Decompress(new DeflateStream(new MemoryStream(payload), CompressionMode.Decompress));

            Console.WriteLine($@"跳过不支持的压缩类型 {compressionType} 的数据块");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解压数据块失败: {ex.Message}");
            return null;
        }
    }

    private static byte[] Decompress(Stream stream)
    {
        using var output = new MemoryStream();
        using (stream) stream.CopyTo(output);
        return output.ToArray();
    }

    // 遍历块内条目（自动跳过末尾的 restart 数组）
    private static List<(byte[] Key, byte[] Value)> ParseBlockEntries(byte[] data)
    {
        var entries = new List<(byte[], byte[])>();
        if (data.Length < 4) return entries;

        var restartCount = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(data.Length - 4));
        var end = (long)data.Length - 4 - 4L * restartCount;
        if (end < 0) return entries;

        var pos = 0;
        var lastKey = Array.Empty<byte>();
        while (pos < end)
        {
            if (!TryReadVarint(data, ref pos, out var shared)) break;
            if (!TryReadVarint(data, ref pos, out var nonShared)) break;
            if (!TryReadVarint(data, ref pos, out var valueLength)) break;

            var sharedLen = (int)shared;
            var nonSharedLen = (int)nonShared;
            var valueLen = (int)valueLength;
            if (pos + nonSharedLen > end || pos + valueLen > end) break;

            var key = new byte[sharedLen + nonSharedLen];
            Array.Copy(lastKey, key, sharedLen);
            Array.Copy(data, pos, key, sharedLen, nonSharedLen);
            pos += nonSharedLen;

            var value = new byte[valueLen];
            Array.Copy(data, pos, value, 0, valueLen);
            pos += valueLen;

            lastKey = key;
            entries.Add((key, value));
        }
        return entries;
    }

    private static bool TryReadBlockHandle(ReadOnlySpan<byte> span, ref int pos, out ulong offset, out ulong size)
    {
        if (!TryReadVarint(span, ref pos, out offset)) { size = 0; return false; }
        if (!TryReadVarint(span, ref pos, out size)) return false;
        return true;
    }

    internal static bool TryReadVarint(ReadOnlySpan<byte> span, ref int pos, out ulong value)
    {
        value = 0;
        var shift = 0;
        while (pos < span.Length)
        {
            var b = span[pos++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return true;
            shift += 7;
            if (shift > 63) return false;
        }
        return false;
    }

    private static bool TryReadLengthPrefixed(ReadOnlySpan<byte> span, ref int pos, out byte[] value)
    {
        value = Array.Empty<byte>();
        if (!TryReadVarint(span, ref pos, out var length)) return false;
        if (pos + (int)length > span.Length) return false;
        value = span.Slice(pos, (int)length).ToArray();
        pos += (int)length;
        return true;
    }

    private static int ReadFully(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read <= 0) break;
            total += read;
        }
        return total;
    }
}
