using System;
using System.IO;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Pack.Theme;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Style;
using Round.SDK.Entity;
using Round.SDK.Helper;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models.Pack.Theme
{
    public class ThemePackMaker
    {
        private readonly ThemePackManifest _manifest;

        public ThemePackMaker(ThemePackManifest manifest)
        {
            _manifest = manifest;
        }

        public async Task StartMake(string savePath)
        {
            var endConf = _manifest;
            var tmpFolder = Path.Combine(PathsList.TempPath, $"theme_{Guid.NewGuid()}");
            Directory.CreateDirectory(tmpFolder);
            Directory.CreateDirectory(Path.Combine(tmpFolder, "background"));
            Directory.CreateDirectory(Path.Combine(tmpFolder, "music"));

            endConf.BackgroundImageBlur = GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur;
            endConf.BackgroundImageOpacity = GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity;
            endConf.BackgroundUse3D = GlobalModel.Config.Data.StyleConfig.Background3D;

            endConf.BackgroundImageFileName = GlobalModel.Config.Data.StyleConfig.BackgroundImage;
            endConf.BackgroundMusicFileName = GlobalModel.Config.Data.StyleConfig.BackgroundMusic;

            endConf.ThemeType = GlobalModel.Config.Data.StyleConfig.LightThemeType;
            endConf.ThemeColor = AccentColor.Colors[GlobalModel.Config.Data.StyleConfig.AccentColorIndex];

            if (File.Exists(endConf.PackIconFileName))
            {
                File.Copy(endConf.PackIconFileName,
                    Path.Combine(tmpFolder, $"pack_icon{Path.GetExtension(endConf.PackIconFileName)}"));
                endConf.PackIconFileName = $"pack_icon{Path.GetExtension(endConf.PackIconFileName)}";
            }

            if (!string.IsNullOrEmpty(endConf.BackgroundImageFileName) && 
                File.Exists(endConf.BackgroundImageFileName))
            {
                File.Copy(endConf.BackgroundImageFileName,
                    Path.Combine(tmpFolder, "background",
                        $"background{Path.GetExtension(endConf.BackgroundImageFileName)}"));
                endConf.BackgroundImageFileName = $"background{Path.GetExtension(endConf.BackgroundImageFileName)}";
            }

            if (!string.IsNullOrEmpty(endConf.BackgroundMusicFileName) && 
                File.Exists(endConf.BackgroundMusicFileName))
            {
                File.Copy(endConf.BackgroundMusicFileName,
                    Path.Combine(tmpFolder, "music",
                        $"background_music{Path.GetExtension(endConf.BackgroundMusicFileName)}"));
                endConf.BackgroundMusicFileName =
                    $"background_music{Path.GetExtension(endConf.BackgroundMusicFileName)}";
            }

            var conf = new ConfigEntity<ThemePackManifest>(Path.Combine(tmpFolder, "manifest.json"));
            conf.Data = endConf;
            conf.Save();
        
            ZipHelper.CreateZipFile(tmpFolder, savePath);
        }
    }
}