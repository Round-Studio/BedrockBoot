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

using System.IO;
using System.IO.Compression;

namespace BedrockBoot.Models.Helper;

/// <summary>
/// 崩溃报告导出：把报告 txt 与转储 dmp 打包成 zip。
/// </summary>
public static class CrashReportExporter
{
    public static bool HasAnything(string? reportPath, string? dumpPath) =>
        (!string.IsNullOrEmpty(reportPath) && File.Exists(reportPath)) ||
        (!string.IsNullOrEmpty(dumpPath) && File.Exists(dumpPath));

    /// <summary>把存在的报告与转储写入 zip 流，返回打包的文件数</summary>
    public static int Pack(string? reportPath, string? dumpPath, Stream output)
    {
        using var zip = new ZipArchive(output, ZipArchiveMode.Create);

        var count = 0;
        if (!string.IsNullOrEmpty(reportPath) && File.Exists(reportPath))
        {
            zip.CreateEntryFromFile(reportPath, Path.GetFileName(reportPath));
            count++;
        }

        if (!string.IsNullOrEmpty(dumpPath) && File.Exists(dumpPath))
        {
            zip.CreateEntryFromFile(dumpPath, Path.GetFileName(dumpPath));
            count++;
        }

        return count;
    }
}
