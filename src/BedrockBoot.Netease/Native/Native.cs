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

using System.Runtime.InteropServices;

namespace BedrockBoot.Netease.Native;

internal class Native
{
    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public unsafe static extern int HttpEncrypt(byte* url, byte* body, out IntPtr buff, out IntPtr key);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public unsafe static extern int ComputeSilence(byte* url, byte* body, byte* data, out IntPtr buff, int bodyLen);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public static extern int ParseLoginResponse(IntPtr pArray, int nSize, out IntPtr buff, out IntPtr key);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public static extern int ComputeDynamicToken(IntPtr urlPtr, int urlSz, IntPtr bodyPtr, int bodySz, out IntPtr buff);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public static extern int GetH5Token(out IntPtr valPtr, out IntPtr buff);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public static extern int HttpDecrypt(IntPtr pArray, int nSize, out IntPtr buff, out IntPtr key);

    [DllImport(CORE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public static extern void FreeMemory(IntPtr ptr);

    public const string CORE_DLL_NAME = "api-ms-win-crt-utility-l1-1-1.dll";
}