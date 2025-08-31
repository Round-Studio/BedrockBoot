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
            var foldFilePath = file.Replace(_integrationInfo.VersionOntologyInfo.BasePath, "");
            File.Copy(file, Path.Combine(WorkFolder, "version",foldFilePath));
            finishCount++;
            progress.Report((finishCount * 0.4,$"复制文件中 ({finishCount}/{fileList.Count})..."));
        });

        #endregion
    }
}