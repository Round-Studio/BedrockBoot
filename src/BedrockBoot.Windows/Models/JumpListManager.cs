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
using System.Diagnostics;
using System.Runtime.InteropServices;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Standard.Interface.Platform;

namespace BedrockBoot.Windows.Models;

public class JumpListManager : IJumpListService
{
    private static readonly Guid CLSID_DestinationList = new("77f10cf0-3db5-4966-b520-b7c54fd35ed6");
    private static readonly Guid CLSID_EnumerableObjectCollection = new("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-c000-000000000046");
    private static Guid IID_IObjectArray = new("92ca9dcd-5622-4bba-a805-5e9f541bd8c9");
    private static PROPERTYKEY PKEY_Title = new()
    {
        fmtid = new Guid("f29f85e0-4ff9-1068-ab91-08002b27b3d9"),
        pid = 2
    };

    void IJumpListService.ConfigureJumpList() => ConfigureJumpList();

    public static void ConfigureJumpList()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            if (!BedrockBoot.Core.Global.GlobalModel.Config.Data.IsTaskBarJumpItem)
            {
                var emptyList = CreateComObject<ICustomDestinationList>(CLSID_DestinationList);
                try
                {
                    var hr = emptyList.BeginList(out _, ref IID_IObjectArray, out var removed);
                    if (hr < 0)
                    {
                        Console.WriteLine($@"清空 JumpList 失败：0x{hr:X8}");
                        return;
                    }

                    Release(removed);
                    emptyList.CommitList();
                }
                finally
                {
                    Release(emptyList);
                }

                return;
            }

            var folders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;
            var index = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex;
            if (index < 0 || index >= folders.Count) return;

            var versions = GameInfoHelper.GetVersionConfigs(folders[index].GameFolderPath);
            if (versions is null || versions.Count == 0) return;

            var collection = CreateComObject<IObjectCollection>(CLSID_EnumerableObjectCollection);
            foreach (var version in versions)
            {
                var link = CreateTaskLink(exePath, $"-jump \"{version.VersionPath}\"", version.Info.VersionName);
                var addHr = collection.AddObject(link);
                if (addHr < 0) Console.WriteLine($@"添加 JumpList 项失败：0x{addHr:X8}");
                Release(link);
            }

            var jumpList = CreateComObject<ICustomDestinationList>(CLSID_DestinationList);
            try
            {
                var hr = jumpList.BeginList(out _, ref IID_IObjectArray, out var removed);
                if (hr < 0)
                {
                    Console.WriteLine($@"创建 JumpList 失败：0x{hr:X8}");
                    return;
                }

                Release(removed);

                hr = jumpList.AddUserTasks(collection);
                if (hr < 0)
                {
                    Console.WriteLine($@"写入 JumpList 失败：0x{hr:X8}");
                    jumpList.AbortList();
                    return;
                }

                hr = jumpList.CommitList();
                if (hr < 0)
                    Console.WriteLine($@"提交 JumpList 失败：0x{hr:X8}");
                else
                    Console.WriteLine(@"添加跳转列表快捷启动项");
            }
            finally
            {
                Release(jumpList);
                Release(collection);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"创建 JumpList 出现错误：{ex}");
        }
    }

    private static object CreateTaskLink(string exePath, string arguments, string title)
    {
        var link = CreateComObject<IShellLinkW>(CLSID_ShellLink);

        link.SetPath(exePath);
        link.SetArguments(arguments);
        link.SetIconLocation(exePath, BedrockBoot.Models.Global.SourceList.MinecraftIconID);

        var titlePtr = Marshal.StringToCoTaskMemUni(title);
        try
        {
            var value = new PROPVARIANT { vt = 31, p = titlePtr };
            var store = (IPropertyStore)link;
            store.SetValue(ref PKEY_Title, ref value);
            store.Commit();
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
        }

        return link;
    }

    private static T CreateComObject<T>(Guid clsid) where T : class
    {
        var type = Type.GetTypeFromCLSID(clsid);
        if (type is null) throw new InvalidOperationException($@"找不到 COM 组件 {clsid}");
        return (T)(Activator.CreateInstance(type) ?? throw new InvalidOperationException($@"无法创建 COM 组件 {clsid}"));
    }

    private static void Release(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
            Marshal.ReleaseComObject(comObject);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr p;
    }

    [ComImport]
    [Guid("6332debf-87b5-4670-90c0-5e57b408a49e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICustomDestinationList
    {
        [PreserveSig] int SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        [PreserveSig]
        int BeginList(out uint pcMinSlots, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [PreserveSig]
        int AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory,
            [MarshalAs(UnmanagedType.Interface)] object poa);

        [PreserveSig] int AppendKnownCategory(int category);

        [PreserveSig] int AddUserTasks([MarshalAs(UnmanagedType.Interface)] object poa);

        [PreserveSig] int CommitList();

        [PreserveSig]
        int GetRemovedDestinations(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [PreserveSig] int DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        [PreserveSig] int AbortList();
    }

    [ComImport]
    [Guid("5632b1a4-e38a-400a-928a-d4cd63230295")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IObjectCollection
    {
        [PreserveSig] int GetCount(out uint pcObjects);

        [PreserveSig]
        int GetAt(uint uiIndex, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [PreserveSig] int AddObject([MarshalAs(UnmanagedType.Interface)] object punk);

        [PreserveSig] int AddFromArray([MarshalAs(UnmanagedType.Interface)] object poaSource);

        [PreserveSig] int RemoveObjectAt(uint uiIndex);

        [PreserveSig] int RemoveAll();
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        [PreserveSig] int GetPath(IntPtr pszFile, int cch, IntPtr pfd, uint fFlags);

        [PreserveSig] int GetIDList(out IntPtr ppidl);

        [PreserveSig] int SetIDList(IntPtr pidl);

        [PreserveSig] int GetDescription(IntPtr pszName, int cch);

        [PreserveSig] int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [PreserveSig] int GetWorkingDirectory(IntPtr pszDir, int cch);

        [PreserveSig] int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        [PreserveSig] int GetArguments(IntPtr pszArgs, int cch);

        [PreserveSig] int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        [PreserveSig] int GetHotkey(out ushort pwHotkey);

        [PreserveSig] int SetHotkey(ushort wHotkey);

        [PreserveSig] int GetShowCmd(out int piShowCmd);

        [PreserveSig] int SetShowCmd(int iShowCmd);

        [PreserveSig] int GetIconLocation(IntPtr pszIconPath, int cch, out int piIcon);

        [PreserveSig] int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        [PreserveSig] int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        [PreserveSig] int Resolve(IntPtr hwnd, uint fFlags);

        [PreserveSig] int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);

        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);

        [PreserveSig] int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);

        [PreserveSig] int SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);

        [PreserveSig] int Commit();
    }
}
