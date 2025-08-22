using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Native
{
    public enum InjectionResult : byte
    {
        Success,
        CommandLineToArgvWFailed,
        GetStdHandleFailed,
        WriteConsoleWFailed,
        IncorrectArguments,
        CreateToolhelp32SnapshotFailed,
        Process32FirstWFailed,
        ProcessNotFound,
        GetFullPathNameWFailed,
        Module32FirstWFailed,
        CopyFileWFailed,
        CreateWellKnownSidFailed,
        GetNamedSecurityInfoWFailed,
        SetEntriesInAclWFailed,
        SetNamedSecurityInfoWFailed,
        OpenProcessFailed,
        VirtualAllocExFailed,
        WriteProcessMemoryFailed,
        CreateRemoteThreadFailed,
        WaitForSingleObjectFailed,
    }
    public static class Injector
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x1F0FFF
        }

        private const string Kernel32 = "kernel32.dll";
        private const string MinecraftAppUserModelId = "Microsoft.MinecraftUWP_8wekyb3d8bbwe!App";
        private const string MinecraftProcessName = "Minecraft.Windows";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
        public static InjectionResult InjectProcess(Process targetProcess, string dllPath)
        {
            string fullDllPath = Path.GetFullPath(dllPath);
            if (!File.Exists(fullDllPath))
                return InjectionResult.GetFullPathNameWFailed;

            try
            {
                var fileInfo = new FileInfo(fullDllPath);
                FileSecurity fs = FileSystemAclExtensions.GetAccessControl(fileInfo);
                var sid = new SecurityIdentifier("S-1-15-2-1");
                fs.AddAccessRule(new FileSystemAccessRule(
                    sid,
                    FileSystemRights.ReadAndExecute,
                    AccessControlType.Allow));
                FileSystemAclExtensions.SetAccessControl(fileInfo, fs);
            }
            catch
            {
                return InjectionResult.SetNamedSecurityInfoWFailed;
            }

            // Prevent duplicate injection
            foreach (ProcessModule module in targetProcess.Modules)
            {
                if (string.Equals(module.FileName, fullDllPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{fullDllPath} already loaded in process.");
                    return InjectionResult.Success;
                }
            }
            IntPtr hTargetProcess = OpenProcess(ProcessAccessFlags.All, false, targetProcess.Id);
            if (hTargetProcess == IntPtr.Zero)
                return InjectionResult.OpenProcessFailed;
            IntPtr remoteAddr = IntPtr.Zero;
            try
            {
                byte[] dllPathBytes = Encoding.Unicode.GetBytes(fullDllPath + "\0");
                uint allocLen = (uint)dllPathBytes.Length;
                remoteAddr = VirtualAllocEx(hTargetProcess, IntPtr.Zero, allocLen, 0x1000 | 0x2000, 0x04);
                if (remoteAddr == IntPtr.Zero)
                    return InjectionResult.VirtualAllocExFailed;
                if (!WriteProcessMemory(hTargetProcess, remoteAddr, dllPathBytes, allocLen, out IntPtr _))
                    return InjectionResult.WriteProcessMemoryFailed;
                IntPtr hKernel32 = GetModuleHandle(Kernel32);
                IntPtr fnLoadLibraryW = GetProcAddress(hKernel32, "LoadLibraryW");
                if (fnLoadLibraryW == IntPtr.Zero)
                    return InjectionResult.CreateRemoteThreadFailed;
                IntPtr hThread = CreateRemoteThread(hTargetProcess, IntPtr.Zero, 0, fnLoadLibraryW, remoteAddr, 0, IntPtr.Zero);
                if (hThread == IntPtr.Zero)
                    return InjectionResult.CreateRemoteThreadFailed;
                using (new SafeProcessHandle(hThread))
                {
                    if (WaitForSingleObject(hThread, 30_000) == 0xFFFFFFFF)
                        return InjectionResult.WaitForSingleObjectFailed;
                }
            }
            finally
            {
                if (remoteAddr != IntPtr.Zero)
                    VirtualFreeEx(hTargetProcess, remoteAddr, 0, 0x8000);
                CloseHandle(hTargetProcess);
            }
            return InjectionResult.Success;
        }

        private sealed class SafeProcessHandle : IDisposable
        {
            private IntPtr handle;
            public SafeProcessHandle(IntPtr h) { handle = h; }
            public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }
        }
    }
}
