using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

#if WINDOWS

public static class ProcessMemoryTrimmer
{
	// Struct definitions
	[StructLayout(LayoutKind.Sequential)]
	private struct LUID
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct LUID_AND_ATTRIBUTES
	{
		public LUID Luid;
		public uint Attributes;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct TOKEN_PRIVILEGES
	{
		public uint PrivilegeCount;
		public LUID_AND_ATTRIBUTES Privilege;
	}
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool K32EmptyWorkingSet(IntPtr hProcess);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr hObject);

	/// <summary>
	/// Trims the working set of the specified process, moving its physical memory pages to the page file.
	/// </summary>
	/// <param name="pid">The PID of the target process.</param>
	/// <returns>Returns true on success, otherwise false.</returns>
	public static bool TrimProcess()
	{
		Process process = Process.GetCurrentProcess();
		var hProcess = process.Handle;

			bool result = K32EmptyWorkingSet(hProcess);
			if (result)
			{
				return true;
			}
			else
			{
				return false;
			}

	}
}

#endif