using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes.Helper.Pack
{
    public class ResourcePackReader
    {
        public static void ReadAnyResourcePacks()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                "LocalState", "games", "com.mojang", "resource_packs");


        }
    }
}
