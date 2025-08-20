using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot
{
    public class JsonCFG : Json_cfg_base
    {
        public string backColor { get; set; }
        public string backimg { get; set; }
        public int DownThread { get; set; } = 12;
        public int DelayTimes { get; set; } = 1000;
        public bool SaveAppx { get; set; } = false; 
        public static JsonCFG FromJson_cfg_base(Json_cfg_base cfg)
        {
            var jsonCfg = new JsonCFG();
            jsonCfg.DevelopMentMode = cfg.DevelopMentMode;
            jsonCfg.appxDir = cfg.appxDir;
            jsonCfg.appxName = cfg.appxName;
            return jsonCfg;
        }
    }
}
