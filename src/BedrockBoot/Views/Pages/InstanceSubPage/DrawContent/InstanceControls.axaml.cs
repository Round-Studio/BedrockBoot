using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Security.Principal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceControls : ISetting
{
    public VersionConfig VersionInfo { get; set; }

    public InstanceControls()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceControls(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "确认删除",
            Content = $"您确定要删除 {VersionInfo.Info.VersionName} ({VersionInfo.Info.Version}) 吗，\n" +
                      $"这将永远无法恢复.jpg",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = (() =>
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = $"删除 {VersionInfo.Info.VersionName}",
                    Content = new DialogDeleteGameContent(VersionInfo)
                });
            }),
            AccountButton = DialogButtons.PrimaryButton
        });
    }

    private async void JumpItemBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var requireAdmin = true; // 管理员权限

        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = requireAdmin ? "保存快捷启动（管理员权限）" : "保存快捷启动",
            SuggestedFileName = $"快捷启动 {VersionInfo.Info.VersionName}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Windows 快捷方式 (.lnk)")
                {
                    Patterns = new[] { "*.lnk" }
                }
            }
        });

        if (file is not null)
        {
            try
            {
                string shortcutPath = file.TryGetLocalPath();

                // 确保路径是 .lnk 扩展名
                if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    shortcutPath = Path.ChangeExtension(shortcutPath, ".lnk");
                }

                var targetPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                var arguments = $"-jump \"{VersionInfo.VersionPath}\"";

                // 使用增强的创建方法，传入是否需要管理员权限
                bool success = CreateShortcutWithComInitialization(shortcutPath, targetPath, arguments, requireAdmin);

                if (success)
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "生成成功",
                        Content = $"快捷方式已成功创建！\n\n" +
                                 $"名称：{Path.GetFileName(shortcutPath)}\n" +
                                 $"位置：{Path.GetDirectoryName(shortcutPath)}",
                        CloseButtonText = "确定",
                        PrimaryButtonText = "打开所在文件夹",
                        PrimaryAction = (() =>
                        {
                            // 打开快捷方式所在文件夹
                            try
                            {
                                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{shortcutPath}\"");
                            }
                            catch { }
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = "创建失败",
                    Content = $"创建快捷方式失败：\n\n{ex.Message}",
                    CloseButtonText = "确定"
                });
            }
        }
    }

    private bool CreateShortcutWithComInitialization(string shortcutPath, string targetPath, 
        string arguments, bool requireAdmin = false)
    {
        try
        {
            // 方法1：使用 Thread STA 模式
            return CreateShortcutInSTAThread(shortcutPath, targetPath, arguments, requireAdmin);
        }
        catch (Exception ex)
        {
            // 方法2：备用方案 - 使用 ShellLink COM 对象
            try
            {
                return CreateShortcutWithShellLink(shortcutPath, targetPath, arguments, requireAdmin);
            }
            catch (Exception ex2)
            {
                // 方法3：使用 PowerShell 创建
                try
                {
                    return CreateShortcutUsingPowerShell(shortcutPath, targetPath, arguments, requireAdmin);
                }
                catch (Exception ex3)
                {
                    throw new Exception($"所有方法失败：\n1. {ex.Message}\n2. {ex2.Message}\n3. {ex3.Message}");
                }
            }
        }
    }

    private bool CreateShortcutInSTAThread(string shortcutPath, string targetPath, 
        string arguments, bool requireAdmin)
    {
        bool success = false;
        Exception threadException = null;

        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var shell = new IWshRuntimeLibrary.WshShell();
                var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
                shortcut.Description = $"BedrockBoot 快捷启动\n{VersionInfo.Info.VersionName}";
                shortcut.IconLocation = $"{targetPath},{SourceList.MinecraftIconID}";

                shortcut.Save();
                
                // 如果需要管理员权限，修改快捷方式属性
                if (requireAdmin)
                {
                    SetRunAsAdminFlag(shortcutPath);
                }

                success = true;
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        // 设置为 STA 线程（COM 需要）
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
            throw threadException;

        return success;
    }

    // COM 接口定义
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd,
            int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
            out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    private bool CreateShortcutWithShellLink(string shortcutPath, string targetPath, 
        string arguments, bool requireAdmin)
    {
        IShellLink link = (IShellLink)new ShellLink();

        // 设置快捷方式属性
        link.SetPath(targetPath);
        link.SetArguments(arguments);
        link.SetWorkingDirectory(Path.GetDirectoryName(targetPath));
        link.SetDescription($"BedrockBoot 快捷启动 - {VersionInfo.Info.VersionName}");
        link.SetIconLocation(targetPath, SourceList.MinecraftIconID);

        // 保存快捷方式
        var persistFile = (IPersistFile)link;
        persistFile.Save(shortcutPath, false);

        // 如果需要管理员权限，修改快捷方式属性
        if (requireAdmin)
        {
            SetRunAsAdminFlag(shortcutPath);
        }

        return true;
    }

    // 方法3：使用 PowerShell 创建快捷方式（最可靠）
    private bool CreateShortcutUsingPowerShell(string shortcutPath, string targetPath, 
        string arguments, bool requireAdmin)
    {
        try
        {
            // 构建 PowerShell 脚本
            string psScript = @$"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath.Replace("'", "''")}')
$Shortcut.TargetPath = '{targetPath.Replace("'", "''")}'
$Shortcut.Arguments = '{arguments.Replace("'", "''")}'
$Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)?.Replace("'", "''")}'
$Shortcut.Description = 'BedrockBoot 快捷启动 - {VersionInfo.Info.VersionName.Replace("'", "''")}'
$Shortcut.IconLocation = '{targetPath.Replace("'", "''")},{SourceList.MinecraftIconID}'
$Shortcut.Save()
";

            // 如果需要管理员权限，添加相应设置
            if (requireAdmin)
            {
                psScript += @$"
# 设置以管理员身份运行
$bytes = [System.IO.File]::ReadAllBytes('{shortcutPath.Replace("'", "''")}')
$bytes[0x15] = $bytes[0x15] -bor 0x20  # 设置 flag 位
[System.IO.File]::WriteAllBytes('{shortcutPath.Replace("'", "''")}', $bytes)
";
            }

            // 创建临时 PowerShell 脚本文件
            string tempScript = Path.GetTempFileName() + ".ps1";
            File.WriteAllText(tempScript, psScript);

            // 执行 PowerShell
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{tempScript}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // 清理临时文件
            File.Delete(tempScript);

            if (process.ExitCode != 0)
            {
                throw new Exception($"PowerShell 执行失败：{error}");
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"PowerShell 创建失败：{ex.Message}");
        }
    }

    // 设置快捷方式以管理员身份运行
    private void SetRunAsAdminFlag(string shortcutPath)
    {
        try
        {
            // 方法1：使用二进制方式修改快捷方式文件
            SetRunAsAdminFlagBinary(shortcutPath);
        }
        catch
        {
            // 方法1失败，尝试方法2
            try
            {
                SetRunAsAdminFlagManifest(shortcutPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"无法设置管理员权限：{ex.Message}");
            }
        }
    }

    // 方法1：直接修改快捷方式文件的二进制数据
    private void SetRunAsAdminFlagBinary(string shortcutPath)
    {
        // 读取文件
        byte[] fileBytes = File.ReadAllBytes(shortcutPath);
        
        // LNK文件格式：偏移0x15处的字节控制运行级别
        // 第6位（0x20）表示"以管理员身份运行"
        if (fileBytes.Length > 0x15)
        {
            fileBytes[0x15] |= 0x20; // 设置标志位
            File.WriteAllBytes(shortcutPath, fileBytes);
        }
        else
        {
            throw new Exception("快捷方式文件格式不正确");
        }
    }

    // 方法2：为快捷方式创建清单文件
    private void SetRunAsAdminFlagManifest(string shortcutPath)
    {
        // 获取目标EXE路径
        string targetPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        
        // 创建清单文件路径
        string manifestPath = targetPath + ".manifest";
        
        // 如果不存在清单文件，创建一个
        if (!File.Exists(manifestPath))
        {
            string manifestContent = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v3"">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level=""requireAdministrator"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>";
            
            File.WriteAllText(manifestPath, manifestContent);
        }
        
        // 创建应用程序清单的快捷方式实际上不起作用，
        // 所以这里我们创建一个批处理文件作为替代方案
        string batPath = Path.ChangeExtension(shortcutPath, ".bat");
        string batContent = $@"
@echo off
REM 检查是否以管理员身份运行
NET SESSION >nul 2>&1
if %errorLevel% == 0 (
    echo 正在以管理员身份运行...
    ""{targetPath}"" {Path.GetFileNameWithoutExtension(shortcutPath).Replace("快捷启动", "").Trim()}
) else (
    echo 请求管理员权限...
    powershell -Command ""Start-Process '{targetPath}' -ArgumentList '{Path.GetFileNameWithoutExtension(shortcutPath).Replace("快捷启动", "").Trim()}' -Verb RunAs""
    exit
)";
        
        File.WriteAllText(batPath, batContent, Encoding.Default);
        
        // 修改原快捷方式指向批处理文件
        var shell = new IWshRuntimeLibrary.WshShell();
        var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = batPath;
        shortcut.Arguments = "";
        shortcut.Save();
    }

    // 方法3：创建 VBScript 来请求管理员权限
    private void CreateAdminShortcutViaVBS(string shortcutPath)
    {
        string vbsPath = Path.ChangeExtension(shortcutPath, ".vbs");
        string targetPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        
        string vbsContent = $@"
Set UAC = CreateObject(""Shell.Application"")
UAC.ShellExecute ""{targetPath}"", ""-jump """"{VersionInfo.VersionPath}"""""", """", ""runas"", {SourceList.MinecraftIconID}
";
        
        File.WriteAllText(vbsPath, vbsContent);
        
        // 修改快捷方式指向 VBS 文件
        var shell = new IWshRuntimeLibrary.WshShell();
        var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = "wscript.exe";
        shortcut.Arguments = $"\"{vbsPath}\"";
        shortcut.Save();
    }

    // 检查当前进程是否以管理员身份运行
    private bool IsRunningAsAdmin()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}