using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot
{
    public class Json_cfg_base
    {
        /// <summary>
        /// config文件版本
        /// </summary>
        public string cfg_ver { get; set; } = Config.cfg_Version.ToString();
        /// <summary>
        /// appx文件夹
        /// </summary>
        public string appxDir { get; set; }
        /// <summary>
        /// appx文件名
        /// </summary>
        public string appxName { get; set; }
        /// <summary>
        /// 开发者模式
        /// </summary>
        public bool DevelopMentMode { get; set; }
    }
}
