using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes.Helper
{
    public class RandomStringGenerator
    {
        // 可用的字符集合（大小写字母 + 数字）
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        // 使用更安全的 RandomNumberGenerator
        public static string GenerateRandomString(int length = 11)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var result = new StringBuilder(length);
            foreach (byte b in randomBytes)
            {
                result.Append(Characters[b % Characters.Length]);
            }

            return result.ToString();
        }
    }
}
