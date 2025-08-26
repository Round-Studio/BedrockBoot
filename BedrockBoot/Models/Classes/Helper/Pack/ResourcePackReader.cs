using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockBoot.Models.Entry.Pack;

namespace BedrockBoot.Models.Classes.Helper.Pack
{
    public class ResourcePackReader
    {
        public static List<ResourcePackManifestEntry> ReadAnyResourcePacks()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                "LocalState", "games", "com.mojang", "resource_packs");
            var res = new List<ResourcePackManifestEntry>();

            if (System.IO.Directory.Exists(minecraftPath))
            {
                Directory.GetDirectories(minecraftPath).ToList().ForEach(folder =>
                {
                    var jsonPath = Path.Combine(folder, "manifest.json");
                    var entry = globalTools.GetJsonFileEntry<ResourcePackManifestEntry>(jsonPath);

                    res.Add(entry);
                });
            }

            return res;
        }
    }
}
