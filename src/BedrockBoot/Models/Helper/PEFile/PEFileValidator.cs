using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BedrockBoot.Models.Helper.PEFile;

public class PEFileValidator
{
    // DOS头结构 - 明确指定 Pack = 1 以确保字节对齐
    [StructLayout(LayoutKind.Sequential, Pack = 1)] // 关键修改：添加 Pack = 1
    public struct IMAGE_DOS_HEADER
    {
        public ushort e_magic; // DOS签名 (0x5A4D) - Offset 0x00
        public ushort e_cblp; // Offset 0x02
        public ushort e_cp; // Offset 0x04
        public ushort e_crlc; // Offset 0x06
        public ushort e_cparhdr; // Offset 0x08
        public ushort e_minalloc; // Offset 0x0A
        public ushort e_maxalloc; // Offset 0x0C
        public ushort e_ss; // Offset 0x0E
        public ushort e_sp; // Offset 0x10
        public ushort e_csum; // Offset 0x12
        public ushort e_ip; // Offset 0x14
        public ushort e_cs; // Offset 0x16
        public ushort e_lfarlc; // Offset 0x18
        public ushort e_ovno; // Offset 0x1A

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1; // Offset 0x1C - 0x23

        public ushort e_oemid; // Offset 0x24
        public ushort e_oeminfo; // Offset 0x26

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2; // Offset 0x28 - 0x35

        public int e_lfanew; // PE头偏移 - Offset 0x3C (60)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)] // 也对其他结构应用 Pack = 1 以保持一致性
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_SECTION_HEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Name;

        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;
    }

    private const ushort DOS_SIGNATURE = 0x5A4D; // "MZ"
    private const uint PE_SIGNATURE = 0x00004550; // "PE\0\0"
    private const ushort NT_OPTIONAL_64_MAGIC = 0x20b;
    private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664; // 64位架构

    /// <summary>
    /// 验证PE文件是否为有效的64位PE文件
    /// </summary>
    /// <param name="filePath">PE文件路径</param>
    /// <returns>如果文件是有效的64位PE文件返回true，否则返回false</returns>
    public static bool IsValidPEFile64(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($@"文件不存在: {filePath}");
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 64)
            {
                Console.WriteLine($@"文件太小，不是一个有效的PE文件: {filePath}");
                return false;
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                var dosHeader = ReadDosHeader(br);

                if (dosHeader.e_magic != DOS_SIGNATURE)
                {
                    Console.WriteLine($@"DOS签名无效: 0x{dosHeader.e_magic:X4} (期望: 0x{DOS_SIGNATURE:X4}) - {filePath}");
                    return false;
                }

                int e_lfanew = dosHeader.e_lfanew;
                // Console.WriteLine($"Debug: e_lfanew read as: {e_lfanew} (0x{e_lfanew:X8})"); // 调试输出

                // 检查 e_lfanew 是否超出合理范围或为负数
                // PE头通常不会在DOS头（64字节）之前，也不会超出文件范围
                if (e_lfanew < 0x40 || e_lfanew >= fileInfo.Length)
                {
                    Console.WriteLine($@"PE头偏移无效或超出文件范围: {e_lfanew} (文件大小: {fileInfo.Length}) - {filePath}");
                    return false;
                }

                fs.Seek(e_lfanew, SeekOrigin.Begin);

                uint peSignature = br.ReadUInt32();
                if (peSignature != PE_SIGNATURE)
                {
                    Console.WriteLine($@"PE签名无效: 0x{peSignature:X8} (期望: 0x{PE_SIGNATURE:X8}) - {filePath}");
                    return false;
                }

                var fileHeader = ReadFileHeader(br);

                if (fileHeader.Machine != IMAGE_FILE_MACHINE_AMD64)
                {
                    Console.WriteLine(
                        $@"机器类型不是64位: 0x{fileHeader.Machine:X4} (期望: 0x{IMAGE_FILE_MACHINE_AMD64:X4}) - {filePath}");
                    return false;
                }

                if (fileHeader.NumberOfSections == 0)
                {
                    Console.WriteLine($@"区段数量为0，不是一个有效的PE文件: {filePath}");
                    return false;
                }

                var optionalHeaderMagic = br.ReadUInt16();
                if (optionalHeaderMagic != NT_OPTIONAL_64_MAGIC)
                {
                    Console.WriteLine(
                        $@"可选头魔数不是64位: 0x{optionalHeaderMagic:X4} (期望: 0x{NT_OPTIONAL_64_MAGIC:X4}) - {filePath}");
                    return false;
                }

                var optionalHeader = ReadOptionalHeader64(br);

                if (optionalHeader.Subsystem == 0 || optionalHeader.Subsystem > 14)
                {
                    Console.WriteLine($@"子系统类型无效: {optionalHeader.Subsystem} - {filePath}");
                    return false;
                }

                if (optionalHeader.ImageBase == 0)
                {
                    Console.WriteLine($@"镜像基址为0 - {filePath}");
                    return false;
                }

                if (optionalHeader.AddressOfEntryPoint == 0)
                {
                    Console.WriteLine($@"入口点地址为0 - {filePath}");
                    return false;
                }

                if (!ValidateSections(br, fileHeader.NumberOfSections, (int)fileInfo.Length))
                {
                    Console.WriteLine($@"区段信息无效: {filePath}");
                    return false;
                }

                Console.WriteLine($@"64位PE文件验证通过: {filePath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"验证64位PE文件时发生错误: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 验证PE文件是否为正常的PE文件
    /// </summary>
    /// <param name="filePath">PE文件路径</param>
    /// <returns>PE文件信息</returns>
    public static bool IsValidEffectivePEFile(string filePath)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                var dosHeader = ReadDosHeader(br);

                if (dosHeader.e_magic != DOS_SIGNATURE)
                {
                    return false;
                }

                int e_lfanew = dosHeader.e_lfanew;

                if (e_lfanew < 0x40 || e_lfanew >= fs.Length)
                {
                    return false;
                }

                fs.Seek(e_lfanew, SeekOrigin.Begin);

                uint peSignature = br.ReadUInt32();
                if (peSignature != PE_SIGNATURE)
                {
                    return false;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 读取DOS头
    /// </summary>
    private static IMAGE_DOS_HEADER ReadDosHeader(BinaryReader br)
    {
        // 由于结构体指定了 Pack = 1，我们需要确保读取的字节数与结构体大小一致
        // DOS头固定大小是64字节 (0x40)
        var buffer = br.ReadBytes(Marshal.SizeOf<IMAGE_DOS_HEADER>()); // 这里 SizeOf 应该返回 64
        if (buffer.Length != 64)
        {
            throw new IOException("无法读取完整的DOS头");
        }

        // 使用 GCHandle 将字节数组转换为结构体
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var header =
                (IMAGE_DOS_HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(IMAGE_DOS_HEADER));
            return header;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// 读取文件头
    /// </summary>
    private static IMAGE_FILE_HEADER ReadFileHeader(BinaryReader br)
    {
        var buffer = br.ReadBytes(Marshal.SizeOf<IMAGE_FILE_HEADER>());
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var header =
                (IMAGE_FILE_HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(IMAGE_FILE_HEADER));
            return header;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// 读取64位可选头 (注意：读取时魔数已经读取了，这里读取剩余部分)
    /// </summary>
    private static IMAGE_OPTIONAL_HEADER64 ReadOptionalHeader64(BinaryReader br)
    {
        var buffer = br.ReadBytes(Marshal.SizeOf<IMAGE_OPTIONAL_HEADER64>() - 2); // 减去已读取的 Magic (2 bytes)
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var header =
                (IMAGE_OPTIONAL_HEADER64)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
                    typeof(IMAGE_OPTIONAL_HEADER64));
            return header;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// 验证区段信息
    /// </summary>
    private static bool ValidateSections(BinaryReader br, ushort numberOfSections, int fileSize)
    {
        for (int i = 0; i < numberOfSections; i++)
        {
            var buffer = br.ReadBytes(Marshal.SizeOf<IMAGE_SECTION_HEADER>());
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var sectionHeader =
                    (IMAGE_SECTION_HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
                        typeof(IMAGE_SECTION_HEADER));

                if (sectionHeader.PointerToRawData != 0 &&
                    sectionHeader.PointerToRawData + sectionHeader.SizeOfRawData > fileSize)
                {
                    Console.WriteLine(
                        $@"区段数据超出文件范围: 偏移={sectionHeader.PointerToRawData}, 大小={sectionHeader.SizeOfRawData}, 文件大小={fileSize}");
                    return false;
                }

                if (sectionHeader.SizeOfRawData > fileSize)
                {
                    Console.WriteLine($@"区段大小超出文件大小: {sectionHeader.SizeOfRawData} > {fileSize}");
                    return false;
                }
            }
            finally
            {
                handle.Free();
            }
        }

        return true;
    }

    /// <summary>
    /// 获取PE文件基本信息（区分32/64位）
    /// </summary>
    /// <param name="filePath">PE文件路径</param>
    /// <returns>PE文件信息</returns>
    public static string GetPEFileInfo(string filePath)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                var dosHeader = ReadDosHeader(br);

                if (dosHeader.e_magic != DOS_SIGNATURE)
                {
                    return "不是有效的PE文件 (DOS签名无效)";
                }

                int e_lfanew = dosHeader.e_lfanew;
                // Console.WriteLine($"Debug: GetPEFileInfo e_lfanew: {e_lfanew} (0x{e_lfanew:X8})"); // 调试输出

                if (e_lfanew < 0x40 || e_lfanew >= fs.Length)
                {
                    return "不是有效的PE文件 (PE头偏移无效)";
                }

                fs.Seek(e_lfanew, SeekOrigin.Begin);

                uint peSignature = br.ReadUInt32();
                if (peSignature != PE_SIGNATURE)
                {
                    return "不是有效的PE文件 (PE签名无效)";
                }

                var fileHeader = ReadFileHeader(br);

                var info = $"PE文件信息:\n";
                info += $"  DOS签名: 0x{dosHeader.e_magic:X4} ({(dosHeader.e_magic == DOS_SIGNATURE ? "有效" : "无效")})\n";
                info += $"  PE头偏移: 0x{e_lfanew:X8}\n"; // 显示实际读取的值
                info += $"  PE签名: 0x{peSignature:X8} ({(peSignature == PE_SIGNATURE ? "有效" : "无效")})\n";
                info += $"  机器类型: 0x{fileHeader.Machine:X4} ({GetMachineTypeName(fileHeader.Machine)})\n";
                info += $"  区段数量: {fileHeader.NumberOfSections}\n";
                info += $"  时间戳: 0x{fileHeader.TimeDateStamp:X8}\n";

                var optionalHeaderMagic = br.ReadUInt16();
                if (optionalHeaderMagic == NT_OPTIONAL_64_MAGIC)
                {
                    info += $"  架构: 64位 (PE32+)\n";
                    var optionalHeader = ReadOptionalHeader64(br);
                    info += $"  镜像基址: 0x{optionalHeader.ImageBase:X16}\n";
                    info += $"  入口点地址: 0x{optionalHeader.AddressOfEntryPoint:X8}\n";
                    info += $"  子系统: {optionalHeader.Subsystem}\n";
                }
                else if (optionalHeaderMagic == 0x10b) // PE32 (32位)
                {
                    info += $"  架构: 32位 (PE32)\n";
                }
                else
                {
                    info += $"  可选头魔数: 0x{optionalHeaderMagic:X4} (未知类型)\n";
                }

                return info;
            }
        }
        catch (Exception ex)
        {
            return $"获取PE文件信息时发生错误: {ex.Message}";
        }
    }

    private static string GetMachineTypeName(ushort machine)
    {
        switch (machine)
        {
            case 0x014c: return "i386 (32位)";
            case 0x8664: return "x64 (64位)";
            case 0x0200: return "Intel Itanium";
            case 0xaa64: return "ARM64";
            default: return $"未知 (0x{machine:X4})";
        }
    }
}