using BedrockBoot.Models.Entry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockBoot.Models.Enum.Background;

namespace BedrockBoot
{
    public class JsonCFG : Json_cfg_base
    {
        public string backColor { get; set; }
        public string backimg { get; set; }
        public int DownThread { get; set; } = 12;
        public int DelayTimes { get; set; } = 1000;
        public bool SaveAppx { get; set; } = true; 
        public bool MouseLock { get; set; } = false;
        public BackgroundEnum BackgroundEnum { get; set; } = BackgroundEnum.Acrylic;
        public int ChooseFolderIndex { get; set; } = 0;

        public List<GameFolderInfoEntry> GameFolders { get; set; } = new List<GameFolderInfoEntry>()
        {
            new GameFolderInfoEntry
            {
                Path = Path.Combine(Config.CFG_DIR, "Bedrock_Data"),
                Name = "默认游戏目录",
                IsAccent = true
            }
        };
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
