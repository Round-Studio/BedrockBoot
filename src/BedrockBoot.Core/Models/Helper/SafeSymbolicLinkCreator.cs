using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace BedrockBoot.Core.Models.Helper
{
    public static class SafeSymbolicLinkCreator
    {
        private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
        private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
        private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public static bool Create(string linkPath, string targetPath, bool isDirectory, bool allowUnprivileged = false)
        {
            if (string.IsNullOrWhiteSpace(linkPath))
                throw new ArgumentException("链接路径不能为空", nameof(linkPath));
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("目标路径不能为空", nameof(targetPath));

            linkPath = Path.GetFullPath(linkPath);
            targetPath = Path.GetFullPath(targetPath);

            if (!isDirectory && !File.Exists(targetPath))
                throw new FileNotFoundException("目标文件不存在", targetPath);
            if (isDirectory && !Directory.Exists(targetPath))
                throw new DirectoryNotFoundException($"目标目录不存在 {targetPath}");

            if (File.Exists(linkPath) || Directory.Exists(linkPath))
            {
                try
                {
                    if (isDirectory)
                        Directory.Delete(linkPath, false);
                    else
                        File.Delete(linkPath);
                }
                catch (Exception ex)
                {
                    throw new IOException($"无法删除已存在的链接 '{linkPath}': {ex.Message}", ex);
                }
            }

            int flags = isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : SYMBOLIC_LINK_FLAG_FILE;
            
            if (allowUnprivileged)
            {
                flags |= SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
            }

            bool success = CreateSymbolicLink(linkPath, targetPath, flags);

            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                
                if (errorCode == 1314)
                {
                     throw new UnauthorizedAccessException(
                         "创建符号链接需要管理员权限，或者在 Windows 设置中启用 '开发者模式'。" +
                         "请以管理员身份运行程序，或在 Windows 设置 > 更新和安全 > 开发者选项中启用开发者模式。");
                }
                
                throw new Win32Exception(errorCode, $"创建符号链接失败: '{linkPath}' -> '{targetPath}'");
            }

            return true;
        }

        public static bool HasPrivilege()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
        
        public static void Delete(string linkPath)
        {
            if (string.IsNullOrWhiteSpace(linkPath))
                throw new ArgumentException("链接路径不能为空", nameof(linkPath));

            linkPath = Path.GetFullPath(linkPath);

            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
            }
            else if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath, false);
            }
            else
            {
                throw new FileNotFoundException("指定的链接路径不存在", linkPath);
            }
        }
    }
}