using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Helper;

public static class ComputeFileMD5
{
    public static async Task<string> ComputeFileMD5Async(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[81920]; // 80KB 缓冲区
                int bytesRead;
                
                // 读取文件的第一部分（除最后一块外）
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                
                // 重要：完成哈希计算
                md5.TransformFinalBlock(buffer, 0, 0);
                
                byte[] hashBytes = md5.Hash; // 现在可以安全获取哈希值
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}