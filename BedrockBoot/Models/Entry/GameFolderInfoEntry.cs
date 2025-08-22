using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Entry
{
    public class GameFolderInfoEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int SelectVersionIndex { get; set; } = 0;
        public bool IsAccent { get; set; } = false; // 参数意思为默认，强制的文件夹，无法删除，并非选中文件夹
    }
}
