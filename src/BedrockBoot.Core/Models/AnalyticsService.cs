using System;
using System.Runtime.InteropServices;
using System.Web;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace BedrockBoot.Core.Models
{
    public class AnalyticsService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://count-bb.roundstudio.top/push";

        public static async Task<bool> PushDeviceLog(string version)
        {
            try
            {
                // 获取设备名称
                string deviceName = Environment.MachineName;
                
                // 获取机器码
                string machineCode = GetMachineCode();
                
                // 组合为 "设备名称_机器码"
                string user = $"{deviceName}_{machineCode}";

                string system = GetOperatingSystemInfo();
                string type = "BedrockBoot";

                var builder = new UriBuilder(BaseUrl);
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["user"] = user;
                query["system"] = system;
                query["version"] = version;
                query["type"] = type;
                builder.Query = query.ToString();

                var response = await _httpClient.GetAsync(builder.ToString());
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取机器码（综合硬件信息生成）
        /// </summary>
        private static string GetMachineCode()
        {
            try
            {
                string cpuId = GetCpuId();
                string diskId = GetDiskId();
                string macAddress = GetMacAddress();
                
                // 组合硬件信息
                string combined = $"{cpuId}{diskId}{macAddress}";
                
                if (string.IsNullOrWhiteSpace(combined))
                {
                    // 如果硬件信息获取失败，使用备用方案
                    combined = Environment.MachineName + Environment.ProcessorCount + Environment.OSVersion.VersionString;
                }
                
                // 计算MD5作为机器码
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                }
            }
            catch
            {
                // 异常时返回基于机器名的简单机器码
                return GenerateFallbackMachineCode();
            }
        }

        /// <summary>
        /// 获取CPU序列号
        /// </summary>
        private static string GetCpuId()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["ProcessorId"]?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 获取硬盘序列号
        /// </summary>
        private static string GetDiskId()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString().Trim() ?? "";
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 获取MAC地址
        /// </summary>
        private static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up && 
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        !nic.Description.ToLower().Contains("virtual") &&
                        !nic.Description.ToLower().Contains("pseudo"))
                    {
                        return nic.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 生成备用机器码（当WMI获取失败时）
        /// </summary>
        private static string GenerateFallbackMachineCode()
        {
            try
            {
                string fallback = Environment.MachineName + 
                                 Environment.ProcessorCount + 
                                 Environment.OSVersion.VersionString +
                                 Environment.UserDomainName;
                
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                }
            }
            catch
            {
                // 最后的备用方案
                return Guid.NewGuid().ToString("N").Substring(0, 16);
            }
        }

        private static string GetOperatingSystemInfo()
        {
            string osDescription = RuntimeInformation.OSDescription;
            string osArchitecture = RuntimeInformation.OSArchitecture.ToString();

            if (osDescription.Contains("Windows"))
            {
                if (osDescription.Contains("10.0") && osDescription.Contains("22000"))
                {
                    return $"Windows 11.{GetOSBuildNumber()}";
                }
                else if (osDescription.Contains("10.0"))
                {
                    return $"Windows 10.{GetOSBuildNumber()}";
                }
            }

            return $"{osDescription} ({osArchitecture})";
        }

        private static string GetOSBuildNumber()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}