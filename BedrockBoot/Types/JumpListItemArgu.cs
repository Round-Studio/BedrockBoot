using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Types
{
    internal class JumpListItemArgu
    {
        Uri Logo { get; set; }
        string Command { get; set; }
        string DisplayName { get; set; }
        string GroupName { get; set; }
        string Description { get; set; }
        public JumpListItemArgu(string DisplayName, string GroupName)
        {
        }
    }
}
