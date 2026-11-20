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
using BedrockBoot.Models.Global;

namespace BedrockBoot.Core.Models.Helper;

/// <summary>
///     PreLoad.NET.dll 的存取层。
///     组件不再随启动器内嵌分发，唯一来源是启动时从 PreLoad.NET-Release 仓库
///     Release 下载到 <see cref="PathsList.PreLoadDllPath"/> 的更新文件；
///     文件缺失时调用方必须跳过释放与注入（原版启动），严禁往主 EXE 写 import。
/// </summary>
public static class PreLoadDllStore
{
    /// <summary>本地是否已有可用的 PreLoad.NET.dll</summary>
    public static bool HasUpdatedDll() =>
        File.Exists(PathsList.PreLoadDllPath) && new FileInfo(PathsList.PreLoadDllPath).Length > 0;

    /// <summary>
    ///     获取应释放到游戏实例的 PreLoad.NET.dll 字节。
    ///     文件缺失时抛出异常，由调用方兜底（跳过注入）。
    /// </summary>
    public static byte[] GetDllBytes() => File.ReadAllBytes(PathsList.PreLoadDllPath);

    /// <summary>
    ///     用下载得到的新 DLL 替换本地文件。
    ///     做轻量校验（AOT 产物应在兆级），防止把截断/错误响应落地后污染释放链路。
    /// </summary>
    public static void ReplaceDll(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 1024 * 1024)
            throw new InvalidOperationException(
                $"PreLoad.NET.dll 内容异常（{bytes?.Length ?? 0} 字节），拒绝替换本地更新文件");

        Directory.CreateDirectory(PathsList.PreLoadPath);

        // 先写临时文件再原子替换，避免写一半被 GetDllBytes 读走
        var tempPath = PathsList.PreLoadDllPath + ".tmp";
        File.WriteAllBytes(tempPath, bytes);
        if (File.Exists(PathsList.PreLoadDllPath)) File.Delete(PathsList.PreLoadDllPath);
        File.Move(tempPath, PathsList.PreLoadDllPath);

        Console.WriteLine($@"PreLoad.NET.dll 更新完毕（{bytes.Length} 字节）");
    }
}
