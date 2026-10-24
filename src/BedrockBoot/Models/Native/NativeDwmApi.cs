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

namespace BedrockBoot.Models.Native;

public static class NativeDwmApi
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public enum DwmSystemBackdropType
    {
        Auto = 0,
        None = 1,
        MicaBase = 2,
        Acrylic = 3,
        MicaAlt = 4
    }

    public static bool SetBackdrop(IntPtr hwnd, DwmSystemBackdropType backdropType)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        int value = (int)backdropType;
        int result = DwmSetWindowAttribute(
            hwnd,
            DWMWA_SYSTEMBACKDROP_TYPE,
            ref value,
            sizeof(int));

        return result == 0;
    }

    public static bool SetImmersiveDarkMode(IntPtr hwnd, bool isDarkMode)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        int value = isDarkMode ? 1 : 0;
        int result = DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref value,
            sizeof(int));

        return result == 0;
    }
}