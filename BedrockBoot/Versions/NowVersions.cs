using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BedrockBoot.Versions
{
    public class NowVersions
    {
        public string BackColor { get; set; }
        public string ImgBack { get; set; }
        public string VersionName { get; set; }
        public string Version_Path { get; set; }
        public string RealVersion { get; set; }
        public string Type { get; set; }

        public bool hasRegeister { get; set; }
        [JsonIgnore]
        public string DisPlayName => $"{VersionName} | {RealVersion}";
    }
 
}
