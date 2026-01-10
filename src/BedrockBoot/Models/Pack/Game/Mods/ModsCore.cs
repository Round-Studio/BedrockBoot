using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Documents;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using PeNet;
using Round.SDK.Helper.IO;

namespace BedrockBoot.Models.Pack.Game.Mods;

public class ModsCore
{
    private ModsManager _manager;

    public VersionConfig VersionInfo { get; set; }
    public List<ModInfo> AllMods { get; private set; }
    public List<ModInfo> PreLoadMods { get; private set; }

    public ModsCore(VersionConfig info)
    {
        VersionInfo = info;
        _manager = new ModsManager(VersionInfo);
    }

    public void PreLoad()
    {
        _manager.RefreshMods();

        var gameConf = Path.Combine(VersionInfo.VersionPath, "game.conf");
        var open = VersionInfo.Config.IsConsole ? "1" : "0";
        File.WriteAllText(gameConf, $"console_open = {open}");

        var rawBody = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row", "Minecraft.Windows.exe");
        var body = Path.Combine(VersionInfo.VersionPath, VersionInfo.BodyFile);
        var preLoadPath = Path.Combine(VersionInfo.VersionPath, "preload");
        var fullPath = Path.Combine(VersionInfo.VersionPath, "PreloadCpp.dll");

        if (!Directory.Exists(Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row")))
            Directory.CreateDirectory(Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "row"));
        if (!Directory.Exists(preLoadPath))
            Directory.CreateDirectory(preLoadPath);

        // 如果raw文件不存在，直接从body复制创建
        if (!File.Exists(rawBody))
        {
            if (File.Exists(body))
            {
                File.Copy(body, rawBody, true);
                Console.WriteLine($"创建了原始备份文件: {rawBody}");
            }
            else
            {
                Console.WriteLine($"源文件不存在: {body}");
                return;
            }
        }

        // 检查body文件是否被占用
        if (!FileCheck.IsFileLocked(body))
        {
            // 比较两个文件是否相同
            if (!AreFilesIdentical(body, rawBody))
            {
                try
                {
                    // 文件内容不同，用raw覆盖body
                    File.Copy(rawBody, body, true);
                    Console.WriteLine($"文件内容不同，已用备份覆盖: {body}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"覆盖文件失败: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("文件内容相同，无需操作");
            }
        }
        else
        {
            Console.WriteLine($"文件被占用，跳过处理: {body}");
        }

        PreLoadMods = new();
        _manager.Mods.ForEach(m =>
        {
            if (m.IsPreLoad)
            {
                PreLoadMods.Add(m);
            }
        });

        Directory.GetFiles(preLoadPath).ToList().ForEach((f) =>
        {
            if (!FileCheck.IsFileLocked(f))
            {
                File.Delete(f);
            }
        });

        PreLoadMods.ForEach(f =>
        {
            if (!FileCheck.IsFileLocked(Path.Combine(preLoadPath, Path.GetFileName(f.File))))
            {
                File.Copy(f.File, Path.Combine(preLoadPath, Path.GetFileName(f.File)));
            }
        });

        try
        {
            if (!FileCheck.IsFileLocked(body) &&
                !FileCheck.IsFileLocked(fullPath))
            {
                using (PeFile peFile = new PeFile(File.Open(body, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                using (var stream = AssetLoader.Open(new Uri("avares://BedrockBoot/Assets/PreloadCpp.dll")))
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    peFile.AddImport("PreloadCpp.dll", "Load");
                    peFile.Flush();
                    try
                    {
                        File.WriteAllBytes(fullPath, memoryStream.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"释放注入 dll 失败：{ex.Message}");
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void LoadAll(int pid) => _manager.InjectAll(pid);

    private static bool AreFilesIdentical(string filePath1, string filePath2)
    {
        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;

        // 如果文件大小不同，直接返回false
        FileInfo info1 = new FileInfo(filePath1);
        FileInfo info2 = new FileInfo(filePath2);

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
    /// 逐字节比较文件（哈希比较失败时的备选方案）
    /// </summary>
    private static bool CompareFilesByteByByte(string filePath1, string filePath2)
    {
        const int bufferSize = 4096 * 4;

        using (var fs1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
        using (var fs2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
        {
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];

            while (true)
            {
                int count1 = fs1.Read(buffer1, 0, bufferSize);
                int count2 = fs2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // 比较读取的字节
                for (int i = 0; i < count1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                        return false;
                }
            }
        }
    }
}