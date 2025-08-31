using BedrockBoot.Integration.Entry;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockBoot.Integration.Classes.Save;

public class SaveIntegration
{
    private string WorkFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoundStudio", "BedrockBoot", "BedrockBoot.Integration", "Save");
    private IntegrationInfo _integrationInfo;
    public SaveIntegration(IntegrationInfo integrationInfo)
    {
        _integrationInfo = integrationInfo;
    }

    public async Task<string> StartMakeAndGetPackPath(IProgress<(double,string)> progress)
    {
        progress.Report((5, "准备文件中..."));
        if (Directory.Exists(WorkFolder)) Directory.Delete(WorkFolder,true);

        Directory.CreateDirectory(WorkFolder);
        Directory.CreateDirectory(Path.Combine(WorkFolder, "worlds"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "d_mods"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "mods"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "version"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "res_packs"));
        progress.Report((10, "初始化文件夹..."));

        var json = JsonSerializer.Serialize(_integrationInfo);
        File.WriteAllText(Path.Combine(WorkFolder, "pack.json"), json);
        progress.Report((15, "初始化版本信息..."));

        #region Copy Version

        progress.Report((20, "准备文件中..."));

        var fileList = Directory.GetFiles(_integrationInfo.VersionOntologyInfo.BasePath, "*",
            SearchOption.AllDirectories).ToList();
        var finishCount = 0;

        fileList.ForEach(file =>
        {
            // 获取相对于基础路径的相对路径
            string relativePath = Path.GetRelativePath(_integrationInfo.VersionOntologyInfo.BasePath, file);

            // 构建目标文件路径
            string targetFile = Path.Combine(WorkFolder, "version", relativePath);

            // 确保目标目录存在
            string targetDirectory = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // 复制文件（覆盖已存在的文件）
            File.Copy(file, targetFile, true);

            finishCount++;
            if (finishCount % 10 == 0)
            {
                progress.Report((((double)finishCount / fileList.Count * 0.4 * 100) + 20,
                    $"复制文件中 ({finishCount}/{fileList.Count})..."));
            }
        });

        if(Directory.Exists(Path.Combine(WorkFolder, "version", "d_mods"))) Directory.Delete(Path.Combine(WorkFolder, "version", "d_mods"), true);
        if (Directory.Exists(Path.Combine(WorkFolder, "version", "mods"))) Directory.Delete(Path.Combine(WorkFolder, "version", "mods"), true);

        #endregion

        #region Copy Mods

        progress.Report((60, "准备文件中..."));


        var dmodsFileList = Directory.GetFiles(Path.Combine(_integrationInfo.VersionOntologyInfo.BasePath, "d_mods"), "*",
            SearchOption.AllDirectories).ToList();

        var modsFileList = Directory.GetFiles(Path.Combine(_integrationInfo.VersionOntologyInfo.BasePath, "mods"), "*",
            SearchOption.AllDirectories).ToList();

        if (_integrationInfo.UseDMods)
        {
            dmodsFileList.ForEach(mod =>
            {
                File.Copy(mod, Path.Combine(WorkFolder, "d_mods", Path.GetFileName(mod)));
                progress.Report((62.5, "准备文件中..."));
            });
        }

        if (_integrationInfo.UseMods)
        {
            modsFileList.ForEach(mod =>
            {
                File.Copy(mod, Path.Combine(WorkFolder, "mods", Path.GetFileName(mod)));
                progress.Report((65, "准备文件中..."));
            });
        }

        #endregion

        #region Copy Worlds

        if (_integrationInfo.UseWorlds)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                "LocalState", "games", "com.mojang", "minecraftWorlds");

            finishCount = 0;
            if (Directory.Exists(minecraftPath))
            {
                var worldFilesList = Directory.GetFiles(minecraftPath, "*", SearchOption.AllDirectories).ToList();

                worldFilesList.ForEach(file =>
                {
                    var worldFileInWorkFolder = Path.GetRelativePath(minecraftPath, file);

                    string targetFile = Path.Combine(WorkFolder, "worlds", worldFileInWorkFolder);
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // 复制文件（覆盖已存在的文件）
                    File.Copy(file, targetFile, true);
                    finishCount++;
                    progress.Report((((double)finishCount / worldFilesList.Count * 0.05 * 100) + 65,
                        $"复制文件中 ({finishCount}/{worldFilesList.Count})..."));
                });
            }
        }

        #endregion

        #region Copy Res Packs

        if (_integrationInfo.UseResPacks)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                "LocalState", "games", "com.mojang", "resource_packs");

            finishCount = 0;
            if (Directory.Exists(minecraftPath))
            {
                var packFilesList = Directory.GetFiles(minecraftPath, "*", SearchOption.AllDirectories).ToList();

                packFilesList.ForEach(file =>
                {
                    var worldFileInWorkFolder = Path.GetRelativePath(minecraftPath, file);

                    string targetFile = Path.Combine(WorkFolder, "res_packs", worldFileInWorkFolder);
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // 复制文件（覆盖已存在的文件）
                    File.Copy(file, targetFile, true);
                    finishCount++;
                    progress.Report((((double)finishCount / packFilesList.Count * 0.05 * 100) + 65,
                        $"复制文件中 ({finishCount}/{packFilesList.Count})..."));
                });
            }
        }

        #endregion

        #region Zip

        var progress_zip = new Progress<(double, string)>(update =>
        {
            progress.Report(((update.Item1 * 0.3)+70, "打包中..."));
        });

        var zipCreator = new ZipCreatorWithProgress();

        var zipFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoundStudio", "BedrockBoot", "BedrockBoot.Integration", "Temp", "Pack_Temp.mcintegation");
        try
        {
            zipCreator.CreateZipFromFolderAsync(
                WorkFolder,
                zipFile,
                progress_zip);
        }
        catch (Exception ex)
        {
            zipFile = String.Empty;
            throw ex;
        }
        progress.Report((100 , "已完成"));

        return zipFile;

        #endregion
    }
}