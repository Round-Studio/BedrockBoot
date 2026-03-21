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