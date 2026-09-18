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