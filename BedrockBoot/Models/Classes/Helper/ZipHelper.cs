using System.IO;
using System.IO.Compression;

namespace BedrockBoot.Models.Classes.Helper;

public class ZipHelper
{
    public static void CreateZip(string zipPath, params string[] filesToZip)
    {
        using (FileStream zipStream = new FileStream(zipPath, FileMode.Create))
        {
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var file in filesToZip)
                {
                    if (File.Exists(file))
                    {
                        string entryName = Path.GetFileName(file);
                        archive.CreateEntryFromFile(file, entryName);
                    }
                }
            }
        }
    }
    public static void CreateZipFromDirectory(string sourceDirectory, string zipPath)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);   
        }
        ZipFile.CreateFromDirectory(sourceDirectory, zipPath);
    }
}