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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BedrockBoot.LevelDb;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;

namespace BedrockBoot.Models.Pack.Game.Archive;

public static class LevelStructureStore
{
    public const string DbKeyPrefix = "structuretemplate_";
    public const string DefaultNamespace = "mystructure";

    public static string GetDbDir(ArchiveInfo info) => Path.Combine(info.Path, "db");

    public static string GetStructuresDir(ArchiveInfo info) => Path.Combine(info.Path, "structures");

    public static bool HasDatabase(ArchiveInfo info) => Directory.Exists(GetDbDir(info));

    // 数据库中的结构（新版 1.21.60+ 由结构方块保存在世界数据库内）
    public static List<McStructureInfo> GetDatabaseStructures(ArchiveInfo info)
    {
        var result = new List<McStructureInfo>();
        var dbDir = GetDbDir(info);
        if (!Directory.Exists(dbDir)) return result;

        var db = new LevelDbDatabase(dbDir);
        foreach (var (key, value) in db.ScanPrefix(DbKeyPrefix))
        {
            result.Add(McStructureInfo.FromDatabase(DbKeyPrefix + key, value, key));
        }
        return result;
    }

    // 旧版结构（1.21.60 之前保存在 structures 文件夹的 .mcstructure 文件）
    public static List<McStructureInfo> GetFileStructures(ArchiveInfo info)
    {
        var result = new List<McStructureInfo>();
        var dir = GetStructuresDir(info);
        if (!Directory.Exists(dir)) return result;

        foreach (var path in Directory.EnumerateFiles(dir, "*.mcstructure", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dir, path).Replace('\\', '/');
            var structureId = Path.ChangeExtension(relative, null).Replace('/', ':');
            result.Add(McStructureInfo.FromFile(path, structureId));
        }
        return result;
    }

    public static List<McStructureInfo> GetAllStructures(ArchiveInfo info)
    {
        IEnumerable<McStructureInfo> structures;
        try
        {
            structures = GetDatabaseStructures(info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取世界数据库结构失败: {ex.Message}");
            structures = new List<McStructureInfo>();
        }

        structures = structures.Concat(GetFileStructures(info));

        return structures
            .OrderBy(x => x.StructureId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Source)
            .ToList();
    }

    public static string NormalizeStructureId(string name)
    {
        return name.Contains(':') ? name : $@"{DefaultNamespace}:{name}";
    }

    public static byte[]? ReadDatabaseStructure(ArchiveInfo info, string dbKey)
    {
        var dbDir = GetDbDir(info);
        if (!Directory.Exists(dbDir)) return null;
        var db = new LevelDbDatabase(dbDir);
        return db.ScanPrefix(dbKey).GetValueOrDefault(string.Empty);
    }

    public static void WriteToDatabase(ArchiveInfo info, IEnumerable<(string StructureId, byte[] Nbt)> structures)
    {
        var ops = structures.Select(s => new LevelDbWriteOp
        {
            Delete = false,
            Key = Encoding.UTF8.GetBytes(DbKeyPrefix + s.StructureId),
            Value = s.Nbt
        }).ToList();

        LevelDbLogAppender.Append(GetDbDir(info), ops);
    }

    public static void DeleteFromDatabase(ArchiveInfo info, string dbKey)
    {
        LevelDbLogAppender.Append(GetDbDir(info),
        [
            new LevelDbWriteOp
            {
                Delete = true,
                Key = Encoding.UTF8.GetBytes(dbKey)
            }
        ]);
    }
}
