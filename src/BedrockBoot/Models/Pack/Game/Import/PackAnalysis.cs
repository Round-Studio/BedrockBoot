using System;
using System.IO;
using System.Text;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Pack.Game.Import;

public class PackAnalysis
{
    public static MinecraftBuildTypeVersion GetPackBuildTypeWithFileHeader(string filePath)
    {
        var header = GetFileHeader(filePath).Replace(" ", "");

        if (header.StartsWith("504B0304"))
            return MinecraftBuildTypeVersion.UWP;

        return MinecraftBuildTypeVersion.GDK;
    }

    public static string GetFileHeader(string filePath, int bytesToRead = 8)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // 读取指定数量的字节
                var headerBytes = reader.ReadBytes(bytesToRead);

                // 将字节转换为十六进制字符串
                var sb = new StringBuilder();
                foreach (var b in headerBytes) sb.AppendFormat("{0:X2} ", b); // X2表示两位大写十六进制

                return sb.ToString().Trim(); // 移除末尾空格
            }
        }
        catch (Exception ex)
        {
            return $"读取文件头失败: {ex.Message}";
        }
    }
}