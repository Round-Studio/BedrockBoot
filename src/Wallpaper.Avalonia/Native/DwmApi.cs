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
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Wallpaper.Avalonia.Native;

/// <summary>
/// DWM API P/Invoke 封装 — 仅在 Windows 上有效。
/// 对应 dwmapi.dll 中的缩略图相关函数。
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DwmApi
{
    // ──────────────────────────────────────────────
    //  常量 (DWM_TNP_*)
    // ──────────────────────────────────────────────
    public const int DWM_TNP_RECTDESTINATION      = 0x0001;
    public const int DWM_TNP_RECTSOURCE           = 0x0002;
    public const int DWM_TNP_OPACITY              = 0x0004;
    public const int DWM_TNP_VISIBLE              = 0x0008;
    public const int DWM_TNP_SOURCECLIENTAREAONLY = 0x0010;

    // ──────────────────────────────────────────────
    //  结构体
    // ──────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public readonly int Width  => right - left;
        public readonly int Height => bottom - top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_THUMBNAIL_PROPERTIES
    {
        public int   dwFlags;
        public RECT  rcDestination;
        public RECT  rcSource;
        public byte  opacity;
        [MarshalAs(UnmanagedType.Bool)]
        public bool  fVisible;
        [MarshalAs(UnmanagedType.Bool)]
        public bool  fSourceClientAreaOnly;
    }

    // ──────────────────────────────────────────────
    //  DllImport
    // ──────────────────────────────────────────────
    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmRegisterThumbnail(
        IntPtr hwndDestination,
        IntPtr hwndSource,
        out IntPtr phThumbnailId);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmUnregisterThumbnail(IntPtr hThumbnailId);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmUpdateThumbnailProperties(
        IntPtr hThumbnailId,
        ref DWM_THUMBNAIL_PROPERTIES ptnProperties);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmIsCompositionEnabled(out bool pfEnabled);

    // ──────────────────────────────────────────────
    //  HRESULT 辅助
    // ──────────────────────────────────────────────
    public static bool Succeeded(int hr) => hr >= 0;
    public static bool Failed(int hr)    => hr < 0;
}
