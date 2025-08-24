using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Native
{
    public static class WindowsApi
    {
        [DllImport("Uwp.Injector.dll",
            CallingConvention = CallingConvention.Cdecl,  // 注意调用约定
            CharSet = CharSet.Ansi,                       // 使用 ANSI (char*)
            SetLastError = true)]
        public static extern int Inject(
            string program,
            string dll_path,
            [MarshalAs(UnmanagedType.Bool)] bool delay_inject,
            int time_m);
        [DllImport("MinimiseFix.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadFix();
    }
}
