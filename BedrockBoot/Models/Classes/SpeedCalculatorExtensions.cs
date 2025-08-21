using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes
{
    public static class SpeedCalculatorExtensions
    {
        public static string ToSpeedString(this double speedMBps)
        {
            if (speedMBps >= 1024)
            {
                return $"{speedMBps / 1024:F2} GB/s";
            }
            else if (speedMBps >= 1)    
            {
                return $"{speedMBps:F2} MB/s";
            }
            else
            {
                return $"{(speedMBps * 1024):F2} KB/s";
            }
        }

        public static string ToFileSizeString(this long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }
    }
}
