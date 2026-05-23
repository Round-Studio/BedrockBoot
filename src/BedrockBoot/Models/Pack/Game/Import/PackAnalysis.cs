using System;
using System.IO;
using System.Text;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Pack.Game.Import;

public class PackAnalysis
{
    public static MinecraftBuildTypeVersion GetPackBuildTypeWithFileHeader(string filePath)
    {
        Console.WriteLine(@"开始分析包文件类型");
        var header = GetFileHeader(filePath).Replace(" ", "");

        if (header.StartsWith("504B0304"))
            return MinecraftBuildTypeVersion.UWP;

        return MinecraftBuildTypeVersion.GDK;
    }

    public static string GetFileHeader(string filePath, int bytesToRead = 8)
    {
        Console.WriteLine($@"获取文件头：{filePath}");
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                var headerBytes = reader.ReadBytes(bytesToRead);

                var sb = new StringBuilder();
                foreach (var b in headerBytes) sb.AppendFormat("{0:X2} ", b);

                Console.WriteLine(@"文件头：{0}", sb);
                return sb.ToString().Trim();
            }
        }
        catch (Exception ex)
        {
            return $"读取文件头失败: {ex.Message}";
        }
    }
}