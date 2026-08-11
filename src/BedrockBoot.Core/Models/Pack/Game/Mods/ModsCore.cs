using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using PeNet;
using PeNet.Header.Pe;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Core.Models.Pack.Game.Mods;

public class ModsCore
{
    private readonly ModsManager _manager;

    public ModsCore(VersionConfig info)
    {
        VersionInfo = info;
        _manager = new ModsManager(VersionInfo);
    }

    public VersionConfig VersionInfo { get; set; }
    public List<ModInfo> AllMods { get; private set; }
    public List<ModInfo> PreLoadMods { get; private set; }

    public void PreLoad()
    {
        _manager.RefreshMods();

        var rawBody = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row", VersionInfo.BodyFile);
        var body = Path.Combine(VersionInfo.VersionPath, VersionInfo.BodyFile);
        var preLoadPath = Path.Combine(VersionInfo.VersionPath, "preload");
        var fullPath = Path.Combine(VersionInfo.VersionPath, "PreLoad.NET.dll");

        if (!Directory.Exists(Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row")))
            Directory.CreateDirectory(Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row"));
        if (!Directory.Exists(preLoadPath))
            Directory.CreateDirectory(preLoadPath);

        try
        {
            if(File.Exists(fullPath)) File.Delete(fullPath);
            File.WriteAllBytes(fullPath,
                Dependence.Dependence.GetResource("BedrockBoot.Dependence.Dependence.PreLoad.NET.dll"));

            Console.WriteLine(@"PreLoad.NET.dll 释放完毕");
        }
        catch(Exception exception) {Console.WriteLine($@"PreLoad.NET.dll 释放失败 {exception}"); }

        // 如果raw文件不存在，直接从body复制创建
        if (!File.Exists(rawBody))
        {
            if (File.Exists(body))
            {
                File.Copy(body, rawBody, true);
                Console.WriteLine($@"创建了原始备份文件: {rawBody}");
            }
            else
            {
                Console.WriteLine($@"源文件不存在: {body}");
                return;
            }
        }

        // 检查body文件是否被占用
        if (!FileCheck.IsFileLocked(body))
        {
            // 比较两个文件是否相同
            if (!AreFilesIdentical(body, rawBody))
                try
                {
                    // 文件内容不同，用raw覆盖body
                    File.Copy(rawBody, body, true);
                    Console.WriteLine($@"文件内容不同，已用备份覆盖: {body}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"覆盖文件失败: {ex.Message}");
                }
            else
                Console.WriteLine(@"文件内容相同，无需操作");
        }
        else
        {
            Console.WriteLine($@"文件被占用，跳过处理: {body}");
        }

        Console.WriteLine($@"当前版本隔离优先级：{GlobalModel.Config.Data.IsolationPriority}");
        if (GlobalModel.Config.Data.IsolationPriority == IsolationModelEnum.Plus)
        {
            if (GameInfoHelper.GetVersionRootFolderType(VersionInfo) == GameFolderType.LeviLauncher)
            {
                try
                {
                    var vcRuntimeFile = Path.Combine(VersionInfo.VersionPath, "vcruntime140_1.dll");
                    if (File.Exists(vcRuntimeFile))
                    {
                        File.Delete(vcRuntimeFile);
                        Console.WriteLine($@"vcruntime140_1.dll 删除成功");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"vcruntime140_1.dll 删除失败; {ex}");
                }
            }
        }

        PreLoadMods = new List<ModInfo>();
        _manager.Mods.ForEach(m =>
        {
            if (m.IsPreLoad) PreLoadMods.Add(m);
        });

        Directory.GetFiles(preLoadPath).ToList().ForEach(f =>
        {
            if (!FileCheck.IsFileLocked(f)) File.Delete(f);
        });

        PreLoadMods.ForEach(f =>
        {
            var file = Path.Combine(preLoadPath, Path.GetFileName(f.File));
            try
            {
                File.Copy(f.File, file);
            }
            catch
            {
            }
        });
        
        Console.WriteLine(@"预加载文件复制完毕");

        try
        {
            if (!FileCheck.IsFileLocked(body))
            {
                using (var fs = new FileStream(body, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                using (var peFile = new PeFile(fs))
                {
                    peFile.AddImport("PreLoad.NET.dll", "DllMain");
                    if (VersionInfo.Config.IsConsole && GlobalModel.Config.Data.IsUseBeta)
                    {
                        if (peFile.ImageNtHeaders != null)
                        {
                            peFile.ImageNtHeaders.OptionalHeader.Subsystem = SubsystemType.WindowsCui;
                            System.Console.WriteLine(@"转换完成！Subsystem 已修改为 WindowsCui (3)");
                        }
                    }
                    peFile.Flush();
                    Console.WriteLine(@"Main EXE 文件修改完毕，已导入 PreLoad.NET.dll");
                }
            }
            else
            {
                Console.WriteLine($@"文件 {body} 被占用，无法修改目标文件");
                throw new IOException($"文件 {body} 被锁定，无法修改");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"修改 PE 文件失败: {ex}");
        }
    }

    public void LoadAll(int pid)
    {
        Console.WriteLine($@"开始注入进程：{pid}");
        _manager.InjectAll(pid);
    }

    private static bool AreFilesIdentical(string filePath1, string filePath2)
    {
        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;

        // 如果文件大小不同，直接返回false
        var info1 = new FileInfo(filePath1);
        var info2 = new FileInfo(filePath2);

        if (info1.Length != info2.Length)
            return false;

        try
        {
            // 计算两个文件的哈希值进行比较
            using (var sha256 = SHA256.Create())
            {
                byte[] hash1, hash2;

                using (var stream1 = File.OpenRead(filePath1))
                {
                    hash1 = sha256.ComputeHash(stream1);
                }

                using (var stream2 = File.OpenRead(filePath2))
                {
                    hash2 = sha256.ComputeHash(stream2);
                }

                return BitConverter.ToString(hash1) == BitConverter.ToString(hash2);
            }
        }
        catch
        {
            // 如果计算哈希失败，回退到逐字节比较
            return CompareFilesByteByByte(filePath1, filePath2);
        }
    }

    /// <summary>
    ///     逐字节比较文件（哈希比较失败时的备选方案）
    /// </summary>
    private static bool CompareFilesByteByByte(string filePath1, string filePath2)
    {
        const int bufferSize = 4096 * 4;

        using (var fs1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
        using (var fs2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
        {
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                var count1 = fs1.Read(buffer1, 0, bufferSize);
                var count2 = fs2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // 比较读取的字节
                for (var i = 0; i < count1; i++)
                    if (buffer1[i] != buffer2[i])
                        return false;
            }
        }
    }
}