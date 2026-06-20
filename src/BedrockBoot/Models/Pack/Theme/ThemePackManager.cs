using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;
using BedrockBoot.Base.Entry.Pack.Theme;
using BedrockBoot.Models.Global;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models.Pack.Theme
{
    public class ThemePackManager
    {
        public ThemePackManager()
        {
            if (!Directory.Exists(PathsList.ThemePath)) Directory.CreateDirectory(PathsList.ThemePath);
        }

        public List<ThemePackManifest> GetPackManifests()
        {
            var folder = PathsList.ThemePath;
            var result = new List<ThemePackManifest>();

            foreach (var pack in Directory.GetFiles(folder, "*.rskin"))
            {
                var analyzer = new ThemePackAnalyze(pack);
                var conf = analyzer.Manifest;
                conf.BackgroundImageFileName = analyzer.GetBackgroundImagePath();
                conf.BackgroundMusicFileName = analyzer.GetBackgroundMusicPath();
                conf.IconData = analyzer.GetPackIconBytes();
                conf.IsSelectThis = pack.Contains(GlobalModel.Config.Data.StyleConfig.SelectThemePackHash);
                result.Add(conf);
            }
        
            return result;
        }
    }
}