using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
            var packFiles = Directory.GetFiles(folder, "*.rskin");
            var result = new List<ThemePackManifest>();
            var lockObj = new object();

            Parallel.ForEach(packFiles, pack =>
            {
                try
                {
                    using var analyzer = new ThemePackAnalyze(pack);
                    var conf = analyzer.Manifest;
                    
                    var backgroundImagePath = analyzer.GetBackgroundImagePath();
                    var backgroundMusicPath = analyzer.GetBackgroundMusicPath();
                    var packIconPath = analyzer.GetPackIconPath();
                    
                    conf.BackgroundImageFileName = string.IsNullOrEmpty(backgroundImagePath) ? null : backgroundImagePath;
                    conf.BackgroundMusicFileName = string.IsNullOrEmpty(backgroundMusicPath) ? null : backgroundMusicPath;
                    conf.PackIconFileName = string.IsNullOrEmpty(packIconPath) ? null : packIconPath;
                    conf.IsSelectThis = GlobalModel.Config.Data.StyleConfig.SelectThemePackHash == conf.PackHash;
                    
                    lock (lockObj)
                    {
                        result.Add(conf);
                    }
                    
                    Console.WriteLine($@"读取到主题包：{conf.PackName} 文件：{pack}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"读取主题包失败：{pack} 错误：{ex}");
                }
            });

            return result.OrderBy(x => x.PackHash ?? "").ToList();
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

        public static ThemePackManifest? GetPackManifestWithHash(string hash)
        {
            var manager = new ThemePackManager();
            return manager.GetPackManifests().FindLast(x => x.PackHash == hash);
        }

        public void CleanupAllCache()
        {
            var cacheDir = PathsList.TempPath;
            if (!Directory.Exists(cacheDir))
                return;

            foreach (var dir in Directory.GetDirectories(cacheDir, "theme_cache_*"))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                }
            }
        }
    }
}