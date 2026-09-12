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