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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockBoot.Core.Models.Helper;

/// <summary>
///     按 MS-SHLLINK 规范直接写出 Windows 快捷方式 (.lnk)。
///     不经过任何 COM interop——发布版的 full trim 会破坏 .NET 内置 COM 支持，
///     WshShell / CoCreateInstance 在裁剪后的单文件包中不可用。
/// </summary>
public static class ShellLinkFile
{
    private const uint LinkFlagsHasIdList = 0x00000001;
    private const uint LinkFlagsIsUnicode = 0x00000080;
    private const uint LinkFlagsHasLinkInfo = 0x00000002;
    private const uint LinkFlagsHasName = 0x00000004;
    private const uint LinkFlagsHasWorkingDir = 0x00000010;
    private const uint LinkFlagsHasArguments = 0x00000020;
    private const uint LinkFlagsHasIconLocation = 0x00000040;

    private const uint LinkInfoFlagsVolumeIdAndLocalBasePath = 0x00000001;
    private const uint LinkInfoHeaderSizeUnicode = 0x00000024;

    private const uint FileAttributeArchive = 0x00000020;
    private const uint ShowCommandNormal = 0x00000001;
    private const uint DriveTypeFixed = 0x00000003;

    private static readonly Guid LinkClsid = new("00021401-0000-0000-C000-000000000046");

    /// <summary>
    ///     创建 .lnk 快捷方式文件。
    /// </summary>
    /// <param name="shortcutPath">快捷方式保存路径，应以 .lnk 结尾</param>
    /// <param name="targetPath">快捷方式指向的目标程序</param>
    /// <param name="arguments">启动参数</param>
    /// <param name="workingDirectory">工作目录</param>
    /// <param name="description">备注</param>
    /// <param name="iconLocation">图标文件路径</param>
    /// <param name="iconIndex">图标在文件中的索引</param>
    public static void Create(string shortcutPath, string targetPath, string arguments = "",
        string workingDirectory = "", string description = "", string iconLocation = "", int iconIndex = 0)
    {
        if (string.IsNullOrEmpty(targetPath))
            throw new ArgumentException("快捷方式目标路径不能为空", nameof(targetPath));

        targetPath = Path.GetFullPath(targetPath);
        if (!string.IsNullOrEmpty(workingDirectory)) workingDirectory = Path.GetFullPath(workingDirectory);

        // LinkInfo（绝对路径解析信息）
        var linkInfo = BuildLinkInfo(targetPath);

        // LinkTargetIDList：目标解析的主要依据（LinkInfo 仅作回退）。
        // 通过 shell 解析目标路径生成；解析失败时降级为仅含 LinkInfo 的结构
        var idList = TryBuildIdList(targetPath);

        // StringData（Unicode 字符串块，按 Header 中标志位出现顺序写入）
        var linkFlags = LinkFlagsIsUnicode | LinkFlagsHasLinkInfo;
        if (idList != null) linkFlags |= LinkFlagsHasIdList;
        var strings = new List<string>();
        if (!string.IsNullOrEmpty(description))
        {
            linkFlags |= LinkFlagsHasName;
            strings.Add(description);
        }

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            linkFlags |= LinkFlagsHasWorkingDir;
            strings.Add(workingDirectory);
        }

        if (!string.IsNullOrEmpty(arguments))
        {
            linkFlags |= LinkFlagsHasArguments;
            strings.Add(arguments);
        }

        if (!string.IsNullOrEmpty(iconLocation))
        {
            linkFlags |= LinkFlagsHasIconLocation;
            strings.Add(iconLocation);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);

        WriteHeader(writer, linkFlags, iconIndex);
        if (idList != null)
        {
            // IDListSize 含末尾 2 字节终止符（与 Windows 实际生成的文件一致）
            writer.Write((ushort)idList.Length);
            writer.Write(idList);
        }

        writer.Write(linkInfo);
        foreach (var str in strings) WriteUnicodeString(writer, str);

        File.WriteAllBytes(shortcutPath, stream.ToArray());
    }

    private static void WriteHeader(BinaryWriter writer, uint linkFlags, int iconIndex)
    {
        writer.Write(0x0000004Cu); // HeaderSize
        writer.Write(LinkClsid.ToByteArray());
        writer.Write(linkFlags);
        writer.Write(FileAttributeArchive);
        writer.Write(0L); // CreationTime
        writer.Write(0L); // AccessTime
        writer.Write(0L); // WriteTime
        writer.Write(0u); // FileSize
        writer.Write(iconIndex);
        writer.Write(ShowCommandNormal);
        writer.Write((ushort)0); // HotKey
        writer.Write((ushort)0); // Reserved1
        writer.Write(0u); // Reserved2
        writer.Write(0u); // Reserved3
    }

    /// <summary>
    ///     用 shell 解析目标路径生成 IDList（ItemID 串 + 2 字节终止符）。
    ///     仅为普通函数调用，不涉及 COM 对象创建，裁剪下安全。
    /// </summary>
    private static byte[] TryBuildIdList(string targetPath)
    {
        try
        {
            SHParseDisplayName(targetPath, IntPtr.Zero, out var pidl, 0, out _);
            try
            {
                if (pidl == IntPtr.Zero) return null;

                // pidl 以 cb=0 的终止符结尾，整体拷贝
                var total = 0;
                var cursor = pidl;
                ushort cb;
                while ((cb = (ushort)Marshal.ReadInt16(cursor)) != 0)
                {
                    total += cb;
                    cursor += cb;
                }

                total += 2; // 终止符
                var result = new byte[total];
                Marshal.Copy(pidl, result, 0, total);
                return result;
            }
            finally
            {
                CoTaskMemFree(pidl);
            }
        }
        catch
        {
            // shell 解析不可用（如非 Windows 环境）时降级为仅 LinkInfo
            return null;
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl,
        uint sfgaoIn, out uint psfgaoOut);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pv);

    private static byte[] BuildLinkInfo(string targetPath)
    {
        // LinkInfoHeaderSize = 0x24，表示携带 LocalBasePathOffsetUnicode / CommonPathSuffixOffsetUnicode。
        // 体积字段按规范顺序线性排布：VolumeID -> LocalBasePath -> CommonPathSuffix
        //   -> LocalBasePathUnicode -> CommonPathSuffixUnicode，全部偏移相对 LinkInfo 起始。
        var ansi = ToAnsi(targetPath);
        var ansiSuffix = new byte[] { 0 }; // 空 CommonPathSuffix
        var unicode = Encoding.Unicode.GetBytes(targetPath + '\0');
        var unicodeSuffix = Encoding.Unicode.GetBytes("\0");
        var volumeId = BuildVolumeId();

        const int headerSize = (int)LinkInfoHeaderSizeUnicode;
        var volumeIdOffset = headerSize;
        var localBasePathOffset = volumeIdOffset + volumeId.Length;
        var commonSuffixOffset = localBasePathOffset + ansi.Length;
        var localBasePathUnicodeOffset = commonSuffixOffset + ansiSuffix.Length;
        var commonSuffixUnicodeOffset = localBasePathUnicodeOffset + unicode.Length;
        var totalSize = commonSuffixUnicodeOffset + unicodeSuffix.Length;

        var info = new byte[totalSize];
        using var writer = new BinaryWriter(new MemoryStream(info), Encoding.UTF8, false);

        writer.Write((uint)totalSize); // LinkInfoSize
        writer.Write(LinkInfoHeaderSizeUnicode);
        writer.Write(LinkInfoFlagsVolumeIdAndLocalBasePath);
        writer.Write((uint)volumeIdOffset);
        writer.Write((uint)localBasePathOffset);
        writer.Write(0u); // CommonNetworkRelativeLinkOffset
        writer.Write((uint)commonSuffixOffset);
        writer.Write((uint)localBasePathUnicodeOffset);
        writer.Write((uint)commonSuffixUnicodeOffset);

        writer.Write(volumeId);
        writer.Write(ansi);
        writer.Write(ansiSuffix);
        writer.Write(unicode);
        writer.Write(unicodeSuffix);

        return info;
    }

    private static byte[] BuildVolumeId()
    {
        // 0x10 头 + 空 ANSI 卷标（仅一个 NULL），VolumeIDSize 必须大于 0x10
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
        writer.Write(0x11u); // VolumeIDSize
        writer.Write(DriveTypeFixed);
        writer.Write(0u); // DriveSerialNumber
        writer.Write(0x10u); // VolumeLabelOffset，指向空卷标
        writer.Write((byte)0);
        return stream.ToArray();
    }

    private static void WriteUnicodeString(BinaryWriter writer, string value)
    {
        // CountCharacters 不含终止符，String 字段本身不带 NULL
        writer.Write((ushort)value.Length);
        writer.Write(Encoding.Unicode.GetBytes(value));
    }

    private static byte[] ToAnsi(string value)
    {
        // ANSI 字段使用系统默认代码页；转码失败的字符按规范无法保真，
        // 但现代 Windows 优先读取 Unicode 字段，这里仅作解析兜底
        var encoding = Encoding.Default;
        return encoding.GetBytes(value + '\0');
    }
}
