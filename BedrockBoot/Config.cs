using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BedrockBoot.Versions;

namespace BedrockBoot
{
    public class Config
    {
        private static string CFG_DIR = Path.Combine(Directory.GetCurrentDirectory(),"BedrockBoot");
        public static Version cfg_Version = new Version("0.0.5");
        public static string CFG_FILE = $"{CFG_DIR}\\config.json";
        public JsonCFG JsonCfg;
        public Config()
        {
            if (!Directory.Exists(CFG_DIR))
            {
                Directory.CreateDirectory(CFG_DIR);
            } 
            if (!File.Exists(CFG_FILE))
            {
             File.Create(CFG_FILE).Dispose();
            var jsonCfg = new JsonCFG();
            jsonCfg.cfg_ver = cfg_Version.ToString();
            jsonCfg.DevelopMentMode = false;
            jsonCfg.appxName = "{0}.appx";
            jsonCfg.appxDir = Directory.GetCurrentDirectory();
            JsonCfg = jsonCfg;
            File.WriteAllText(CFG_FILE, JsonSerializer.Serialize(jsonCfg));
            }else {
                var readAllText = File.ReadAllText(CFG_FILE);
                var jsonCfgBase = JsonSerializer.Deserialize<Json_cfg_base>(readAllText);
                if (cfg_Version > new Version(jsonCfgBase.cfg_ver))
                {
                    JsonCFG cfg = JsonCFG.FromJson_cfg_base(jsonCfgBase);
                    File.WriteAllText(CFG_FILE,JsonSerializer.Serialize(cfg));
                    JsonCfg = cfg;
                    return;
                }
                else if (cfg_Version == new Version(jsonCfgBase.cfg_ver))
                {
                    var jsonCfg = JsonSerializer.Deserialize<JsonCFG>(readAllText);
                    JsonCfg = jsonCfg;
                }
            }
        }
        public void SaveConfig()
        {
            File.WriteAllTextAsync(CFG_FILE,JsonSerializer.Serialize(JsonCfg));
        }
    }
}
