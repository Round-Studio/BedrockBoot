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
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Helper;

public static class ComputeFileMD5
{
    public static async Task<string> ComputeFileMD5Async(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var buffer = new byte[81920]; // 80KB 缓冲区
                int bytesRead;

                // 读取文件的第一部分（除最后一块外）
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    md5.TransformBlock(buffer, 0, bytesRead, null, 0);

                // 重要：完成哈希计算
                md5.TransformFinalBlock(buffer, 0, 0);

                var hashBytes = md5.Hash; // 现在可以安全获取哈希值
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}