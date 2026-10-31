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

using System;
using System.IO;
using System.Text;

namespace BedrockBoot.Standard.Entity.Game.Pack.Archive;

public enum StructureSource
{
    Database,
    File
}

public class McStructureInfo
{
    public string FilePath { get; private set; } = string.Empty;
    public string StructureId { get; private set; } = string.Empty;
    public string DbKey { get; private set; } = string.Empty;
    public StructureSource Source { get; private set; }
    public int SizeX { get; private set; }
    public int SizeY { get; private set; }
    public int SizeZ { get; private set; }
    public int PaletteCount { get; private set; }
    public long FileSize { get; private set; }
    public DateTime ModifiedTime { get; private set; }
    public bool IsValid { get; private set; } = true;

    public long BlockCount => (long)SizeX * SizeY * SizeZ;

    public static McStructureInfo FromFile(string filePath, string structureId)
    {
        var info = new McStructureInfo
        {
            FilePath = filePath,
            StructureId = structureId,
            Source = StructureSource.File,
            ModifiedTime = File.GetLastWriteTime(filePath)
        };

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            info.FileSize = stream.Length;
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            info.Parse(reader);
        }
        catch
        {
            info.IsValid = false;
        }

        return info;
    }

    public static McStructureInfo FromDatabase(string dbKey, byte[] nbt, string structureId)
    {
        var info = new McStructureInfo
        {
            DbKey = dbKey,
            StructureId = structureId,
            Source = StructureSource.Database,
            FileSize = nbt.Length
        };

        try
        {
            using var stream = new MemoryStream(nbt);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            info.Parse(reader);
        }
        catch
        {
            info.IsValid = false;
        }

        return info;
    }

    // .mcstructure 为无压缩小端 NBT，仅沿 structure.palette.default 链下钻以避免物化巨大的 block_indices
    private void Parse(BinaryReader reader)
    {
        var rootType = reader.ReadByte();
        if (rootType != 10) throw new InvalidDataException("不是有效的 mcstructure 文件");
        ReadShortString(reader);
        ReadCompound(reader, string.Empty);
    }

    private void ReadCompound(BinaryReader reader, string path)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var type = reader.ReadByte();
            if (type == 0) return;
            var name = ReadShortString(reader);
            ReadValue(reader, type, path.Length == 0 ? name : $"{path}.{name}");
        }
    }

    private void ReadValue(BinaryReader reader, byte type, string path)
    {
        switch (type)
        {
            case 9:
            {
                var elemType = reader.ReadByte();
                var count = reader.ReadInt32();
                if (path == "size" && elemType == 3 && count >= 3)
                {
                    SizeX = reader.ReadInt32();
                    SizeY = reader.ReadInt32();
                    SizeZ = reader.ReadInt32();
                    Skip(reader, (count - 3) * 4L);
                }
                else
                {
                    if (path == "structure.palette.default.block_palette") PaletteCount = count;
                    SkipList(reader, elemType, count);
                }
                break;
            }
            case 10:
                if (path is "structure" or "structure.palette" or "structure.palette.default")
                    ReadCompound(reader, path);
                else
                    SkipValue(reader, 10);
                break;
            default:
                SkipValue(reader, type);
                break;
        }
    }

    private void SkipList(BinaryReader reader, byte elemType, int count)
    {
        switch (elemType)
        {
            case 1: Skip(reader, count); break;
            case 2:
            case 5: Skip(reader, count * 2L); break;
            case 3: Skip(reader, count * 4L); break;
            case 4:
            case 6: Skip(reader, count * 8L); break;
            default:
                for (var i = 0; i < count; i++) SkipValue(reader, elemType);
                break;
        }
    }

    private void SkipValue(BinaryReader reader, byte type)
    {
        switch (type)
        {
            case 1: Skip(reader, 1); break;
            case 2: Skip(reader, 2); break;
            case 3:
            case 5: Skip(reader, 4); break;
            case 4:
            case 6: Skip(reader, 8); break;
            case 7: Skip(reader, reader.ReadInt32()); break;
            case 8: Skip(reader, reader.ReadUInt16()); break;
            case 9:
            {
                var elemType = reader.ReadByte();
                var count = reader.ReadInt32();
                SkipList(reader, elemType, count);
                break;
            }
            case 10: SkipCompound(reader); break;
            case 11: Skip(reader, reader.ReadInt32() * 4L); break;
            case 12: Skip(reader, reader.ReadInt64() * 8L); break;
        }
    }

    private void SkipCompound(BinaryReader reader)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var type = reader.ReadByte();
            if (type == 0) return;
            Skip(reader, reader.ReadUInt16());
            SkipValue(reader, type);
        }
    }

    private static string ReadShortString(BinaryReader reader)
    {
        var len = reader.ReadUInt16();
        return len == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    private static void Skip(BinaryReader reader, long count)
    {
        if (count > 0) reader.BaseStream.Seek(count, SeekOrigin.Current);
    }
}
