using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot
{
    public class ModManager
    {
        public  ObservableCollection<DllFileInfo> ModsList = new ();
        public static ModManager Instance { get; } = new ModManager();
        private ModManager()
        {
            
        }
        public bool RemoveMod(DllFileInfo modDetils)
        {
            try
            {
                if (!File.Exists(modDetils.FullPath))
                {
                    return false;
                }
                File.Delete(modDetils.FullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public List<string> GetInstalledMods()
        {
            // 获取已安装模组的逻辑
            return new List<string>();
        }
    }

}
