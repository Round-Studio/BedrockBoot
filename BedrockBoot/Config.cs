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
        public static Version cfg_Version = new Version("0.0.1");
        public static string CFG_FILE = $"{CFG_DIR}config.json";
        public static string DATA_FILE = $"{CFG_DIR}data.json";
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

            if (!File.Exists(DATA_FILE))
            {
                File.WriteAllText(DATA_FILE,
                    JsonSerializer.Serialize(new DATAVersion() { VersionsList = new List<NowVersions>() }));
                global_cfg.VersionsList = new List<NowVersions>();
            }
            else
            {
                global_cfg.VersionsList = new List<NowVersions>();
                var s = File.ReadAllText(DATA_FILE);
                var dataVersion = JsonSerializer.Deserialize<DATAVersion>(s);
                global_cfg.VersionsList = dataVersion.VersionsList;
            }
        }

        public void SaveVersion(NowVersions ver)
        {
            File.WriteAllTextAsync(DATA_FILE,JsonSerializer.Serialize(new DATAVersion(){VersionsList = global_cfg.VersionsList}));
        }
    }
}
