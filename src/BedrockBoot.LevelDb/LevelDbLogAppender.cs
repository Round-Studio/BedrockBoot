using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BedrockBoot.LevelDb;

public sealed class LevelDbWriteOp
{
    public required bool Delete { get; init; }
    public required byte[] Key { get; init; }
    public byte[]? Value { get; init; }
}

// 通过向当前 WAL 追加标准 WriteBatch 记录来修改数据库，游戏下次打开存档时会原样重放这些记录。
// 必须在游戏关闭后调用；追加前会把 CURRENT/MANIFEST/日志 备份到 db/.bb_backup。
public static class LevelDbLogAppender
{
    public static void Append(string dbDir, IReadOnlyList<LevelDbWriteOp> ops)
    {
        if (ops.Count == 0) return;

        var db = new LevelDbDatabase(dbDir);
        var manifest = db.Manifest;
        if (manifest.Comparator is { } comparator && comparator != "leveldb.BytewiseComparator")
            throw new NotSupportedException($@"不支持的键比较器: {comparator}");

        var logPath = FindActiveLog(dbDir, manifest.LogNumber);

        EnsureWritable(dbDir, logPath);

        var sequence = Math.Max(manifest.LastSequence, db.GetMaxLogSequence()) + 1;
        Backup(dbDir, manifest, logPath);

        var batch = EncodeBatch(sequence, ops);
        using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            EmitRecord(fs, batch);
            fs.Flush(true);
        }
    }

    private static string FindActiveLog(string dbDir, ulong logNumber)
    {
        var logs = Directory.GetFiles(dbDir, "*.log")
            .Select(p => (Path: p, Number: ulong.TryParse(Path.GetFileNameWithoutExtension(p), out var n) ? n : 0UL))
            .Where(x => x.Number >= logNumber)
            .OrderByDescending(x => x.Number)
            .ToList();

        if (logs.Count == 0)
            throw new FileNotFoundException($@"找不到可用的数据库日志文件（logNumber={logNumber}）");
        return logs[0].Path;
    }

    private static void EnsureWritable(string dbDir, string logPath)
    {
        try
        {
            using (new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            var lockPath = Path.Combine(dbDir, "LOCK");
            if (File.Exists(lockPath))
                using (new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
        }
        catch (Exception ex)
        {
            throw new IOException("世界数据库被占用，请先关闭游戏后再修改结构", ex);
        }
    }

    private static void Backup(string dbDir, LevelDbManifest manifest, string logPath)
    {
        try
        {
            var dir = Path.Combine(dbDir, ".bb_backup", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(dir);
            foreach (var name in new[] { "CURRENT", Path.GetFileName(manifest.ManifestPath), Path.GetFileName(logPath) })
                File.Copy(Path.Combine(dbDir, name), Path.Combine(dir, name), true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"备份数据库失败: {ex.Message}");
        }
    }

    private static byte[] EncodeBatch(ulong sequence, IReadOnlyList<LevelDbWriteOp> ops)
    {
        using var ms = new MemoryStream();
        Span<byte> header = stackalloc byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(header, sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], (uint)ops.Count);
        ms.Write(header);

        foreach (var op in ops)
        {
            ms.WriteByte(op.Delete ? (byte)0 : (byte)1);
            WriteLengthPrefixed(ms, op.Key);
            if (!op.Delete) WriteLengthPrefixed(ms, op.Value!);
        }
        return ms.ToArray();
    }

    private static void WriteLengthPrefixed(Stream stream, byte[] data)
    {
        Span<byte> buffer = stackalloc byte[10];
        var length = WriteVarint(buffer, (ulong)data.Length);
        stream.Write(buffer[..length]);
        stream.Write(data);
    }

    private static int WriteVarint(Span<byte> buffer, ulong value)
    {
        var pos = 0;
        while (value >= 0x80)
        {
            buffer[pos++] = (byte)(value | 0x80);
            value >>= 7;
        }
        buffer[pos++] = (byte)value;
        return pos;
    }

    // 与 leveldb db/log_writer.cc 完全一致的记录分块与 CRC 掩码
    private static void EmitRecord(FileStream fs, byte[] data)
    {
        var blockPos = (int)(fs.Length % LevelDbDatabase.BlockSize);
        var offset = 0;
        var left = data.Length;
        var begin = true;

        do
        {
            var leftover = LevelDbDatabase.BlockSize - blockPos;
            if (leftover < LevelDbDatabase.LogHeaderSize)
            {
                if (leftover > 0) fs.Write(new byte[leftover]);
                blockPos = 0;
            }

            var available = LevelDbDatabase.BlockSize - blockPos - LevelDbDatabase.LogHeaderSize;
            var fragment = Math.Min(left, available);

            byte type;
            if (begin && fragment == left) type = 1;
            else if (begin) type = 2;
            else if (fragment == left) type = 4;
            else type = 3;

            EmitPhysicalRecord(fs, type, data.AsSpan(offset, fragment));
            blockPos += LevelDbDatabase.LogHeaderSize + fragment;
            offset += fragment;
            left -= fragment;
            begin = false;
        } while (left > 0);
    }

    private static void EmitPhysicalRecord(FileStream fs, byte type, ReadOnlySpan<byte> payload)
    {
        Span<byte> header = stackalloc byte[LevelDbDatabase.LogHeaderSize];
        var crc = Crc32C.Extend(Crc32C.Value([type]), payload);
        BinaryPrimitives.WriteUInt32LittleEndian(header, Crc32C.Mask(crc));
        header[4] = (byte)payload.Length;
        header[5] = (byte)(payload.Length >> 8);
        header[6] = type;
        fs.Write(header);
        fs.Write(payload);
    }
}
