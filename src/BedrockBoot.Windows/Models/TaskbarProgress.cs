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

namespace BedrockBoot.Windows.Models;

public static class TaskbarProgress
{
    private static ITaskbarList3? _taskbarList;
    private static bool _initialized;
    private static readonly object _lock = new();

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            try
            {
                var clsid = new Guid("56FDF344-FD6D-11d0-958A-006097C9A090");
                var type = Type.GetTypeFromCLSID(clsid);
                if (type != null)
                {
                    var instance = (ITaskbarList3)Activator.CreateInstance(type)!;
                    instance.HrInit();
                    _taskbarList = instance;
                }
            }
            catch
            {
            }
            _initialized = true;
        }
    }

    public static void SetProgress(IntPtr windowHandle, double progress, bool hasTasks)
    {
        EnsureInitialized();
        if (_taskbarList == null || windowHandle == IntPtr.Zero) return;

        if (!hasTasks || progress >= 100)
        {
            _taskbarList.SetProgressState(windowHandle, TBPFLAG.TBPF_NOPROGRESS);
        }
        else if (progress <= 0)
        {
            _taskbarList.SetProgressState(windowHandle, TBPFLAG.TBPF_INDETERMINATE);
        }
        else
        {
            var value = (ulong)Math.Clamp(progress, 0, 100);
            _taskbarList.SetProgressValue(windowHandle, value, 100);
            _taskbarList.SetProgressState(windowHandle, TBPFLAG.TBPF_NORMAL);
        }
    }

    [ComImport]
    [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
        void MarkFullscreenWindow(IntPtr hwnd, bool fullscreen);
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
    }

    private enum TBPFLAG
    {
        TBPF_NOPROGRESS = 0,
        TBPF_INDETERMINATE = 0x1,
        TBPF_NORMAL = 0x2,
        TBPF_ERROR = 0x4,
        TBPF_PAUSED = 0x8
    }
}
