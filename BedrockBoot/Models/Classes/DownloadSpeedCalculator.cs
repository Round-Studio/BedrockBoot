using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes
{
    public class DownloadSpeedCalculator
    {
        private long _lastDownloadedBytes;
        private DateTime _lastTime;
        private readonly object _lock = new object();

        public DownloadSpeedCalculator()
        {
            _lastTime = DateTime.Now;
            _lastDownloadedBytes = 0;
        }

        public double CalculateSpeed(long downloadedBytes, long totalBytes)
        {
            lock (_lock)
            {
                DateTime currentTime = DateTime.Now;
                double elapsedSeconds = (currentTime - _lastTime).TotalSeconds;

                if (elapsedSeconds <= 0) return 0;

                long bytesDiff = downloadedBytes - _lastDownloadedBytes;
                double speedMBps = (bytesDiff / elapsedSeconds) / (1024 * 1024); // 转换为 MB/s

                _lastDownloadedBytes = downloadedBytes;
                _lastTime = currentTime;

                return speedMBps;
            }
        }
    }
}
