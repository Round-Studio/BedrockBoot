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

    public void StartMake(IProgress<(double,string)> progress)
    {
        progress.Report((5, "准备文件中..."));
        if (Directory.Exists(WorkFolder)) Directory.Delete(WorkFolder,true);

        Directory.CreateDirectory(WorkFolder);
        Directory.CreateDirectory(Path.Combine(WorkFolder, "worlds"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "d_mods"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "mods"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "version"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "res_packs"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "res_packs", "resource_packs"));
        Directory.CreateDirectory(Path.Combine(WorkFolder, "res_packs", "behavior_packs"));
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
            progress.Report((((double)finishCount / fileList.Count * 0.4 * 100) + 20,
                $"复制文件中 ({finishCount}/{fileList.Count})..."));
        });

        #endregion
    }
}