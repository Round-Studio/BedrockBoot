using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Types
{
    class VersionType
    {
        public string Version { get; set; } = "ERROR VERSION";
        public string Date { get; set; } = "ERROR DATE";
        public string Icon { get; set; } = "\uEA39"; // 这里用WinUI样式，不是XAML！
    }
}
