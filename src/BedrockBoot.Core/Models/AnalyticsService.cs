using System;
using System.Runtime.InteropServices;
using System.Web;

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
                string user = Environment.MachineName;

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