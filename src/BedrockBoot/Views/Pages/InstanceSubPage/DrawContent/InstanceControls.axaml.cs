using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
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
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存快捷启动",
            SuggestedFileName = $"快捷启动 {VersionInfo.Info.VersionName}",
            // 注意：快捷方式扩展名应该是 .lnk，不是 .ink
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

                // 使用增强的创建方法
                bool success = CreateShortcutWithComInitialization(shortcutPath, targetPath, arguments);

                if (success)
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "生成成功",
                        Content = $"快捷方式已成功创建：\n{Path.GetFileName(shortcutPath)}",
                        CloseButtonText = "确定"
                    });
                }
            }
            catch (Exception ex)
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = "错误",
                    Content = $"创建快捷方式失败：\n{ex.Message}",
                    CloseButtonText = "确定"
                });
                // 不再抛出异常，避免程序崩溃
                // throw new Exception($"创建带参数的快捷方式失败：{ex.Message}");
            }
        }
    }

    private bool CreateShortcutWithComInitialization(string shortcutPath, string targetPath, string arguments)
    {
        try
        {
            // 方法1：使用 Thread STA 模式
            return CreateShortcutInSTAThread(shortcutPath, targetPath, arguments);
        }
        catch (Exception ex)
        {
            // 方法2：备用方案 - 使用 ShellLink COM 对象
            try
            {
                return CreateShortcutWithShellLink(shortcutPath, targetPath, arguments);
            }
            catch (Exception ex2)
            {
                throw new Exception($"所有方法失败：\n{ex.Message}\n{ex2.Message}");
            }
        }
    }

    private bool CreateShortcutInSTAThread(string shortcutPath, string targetPath, string arguments)
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
                shortcut.IconLocation = $"{targetPath},1";

                shortcut.Save();
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

// 添加这些 COM 接口定义
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

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

    private bool CreateShortcutWithShellLink(string shortcutPath, string targetPath, string arguments)
    {
        IShellLink link = (IShellLink)new ShellLink();

        // 设置快捷方式属性
        link.SetPath(targetPath);
        link.SetArguments(arguments);
        link.SetWorkingDirectory(Path.GetDirectoryName(targetPath));
        link.SetDescription($"BedrockBoot 快捷启动 - {VersionInfo.Info.VersionName}");
        link.SetIconLocation(shortcutPath, 1);

        // 保存快捷方式
        var persistFile = (IPersistFile)link;
        persistFile.Save(shortcutPath, false);

        return true;
    }
}