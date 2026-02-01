using System.Runtime.InteropServices;
using Native;

namespace BedrockBoot.Inject;

public static class Native
{
    private static MemoryModule memoryModule;

    public static void Init(byte[] bytes)
    {
        memoryModule = new MemoryModule(bytes);
    }

    public static unsafe void LoadPlugins(int targetPid, string dll_path, bool delay_inject, int time_ms)
    {
        var ptr = memoryModule.GetPtr("Inject");
        var func = (delegate* unmanaged[Cdecl]<int, void*, bool, int, int>)ptr;
        var address = Marshal.StringToHGlobalAnsi(dll_path);
        func(targetPid, address.ToPointer(), delay_inject, time_ms);
    }
}