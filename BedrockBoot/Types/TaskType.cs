using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Types
{
    public class TaskType
    {
        public string Header { get; set; } = "Header";
        public string Description { get; set; } = "Description";
        public double Progress { get; set; } = 0;
    }
}
