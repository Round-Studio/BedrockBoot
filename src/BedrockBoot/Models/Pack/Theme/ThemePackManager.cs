using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
                conf.PackIconFileName = analyzer.GetPackIconPath();
                conf.IsSelectThis = pack.Contains(GlobalModel.Config.Data.StyleConfig.SelectThemePackHash);
                result.Add(conf);
                
                Console.WriteLine($@"读取到主题包：{conf.PackName} 文件：{pack}");
            }
        
            return result;
        }

        public void AddPack(string selectedPath)
        {
            var hash = ComputeFileHash(selectedPath);
            File.Copy(selectedPath, Path.Combine(PathsList.ThemePath, $"{hash}.rskin"), true);
        }

        private string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
    }
}