using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Isolation;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;

namespace BedrockBoot.Models.Pack.Game.Isolation;

public class IsolationMigration
{
    public IsolationMigration() {}

    public VersionConfig NewConfig { get; set; }
    public VersionConfig OldConfig { get; set; }
    public IProgress<MigrationProgress>? MigrationProgress { get; set; }

    public async Task MigrateFoldersAsync(MigrationConfig migrationConfig)
    {
        var dirsEnable = new List<(bool isEnabled, string newPath, string oldPath)>()
        {
            (migrationConfig.IsEnableArchive,
                IsolationCore.GetInstanceFolderPath(migrationConfig.NewVersionConfig, InstanceFolderType.ArchiveFolder),
                IsolationCore.GetInstanceFolderPath(migrationConfig.OldVersionConfig, InstanceFolderType.ArchiveFolder)),
            (migrationConfig.IsEnableConfig,
                IsolationCore.GetInstanceFolderPath(migrationConfig.NewVersionConfig, InstanceFolderType.OptionFolder),
                IsolationCore.GetInstanceFolderPath(migrationConfig.OldVersionConfig, InstanceFolderType.OptionFolder)),
            (migrationConfig.IsEnableResourcePack,
                IsolationCore.GetInstanceFolderPath(migrationConfig.NewVersionConfig, InstanceFolderType.ResourcePackFolder),
                IsolationCore.GetInstanceFolderPath(migrationConfig.OldVersionConfig, InstanceFolderType.ResourcePackFolder)),
            (migrationConfig.IsEnableBehaviorPack,
                IsolationCore.GetInstanceFolderPath(migrationConfig.NewVersionConfig, InstanceFolderType.BehaviorPackFolder),
                IsolationCore.GetInstanceFolderPath(migrationConfig.OldVersionConfig, InstanceFolderType.BehaviorPackFolder))
        };
        
        var enabledItems = dirsEnable.Where(x => x.isEnabled).ToList(); // 获取上面这坨启用的项
        var filesCount = enabledItems
            .Sum(item => Directory.Exists(item.oldPath)
                ? Directory.GetFiles(item.oldPath, "*", SearchOption.AllDirectories).Length
                : 0);

        MigrationProgress.Report(new MigrationProgress
        {
            FileCountTotal = filesCount,
            CurrentFile = 0,
            Status = "开始迁移",
            CurrentType = ""
        });

        var processedFiles = 0;

        foreach (var item in enabledItems)
        {
            var oldPath = item.oldPath;
            var newPath = item.newPath;
            var currentType = GetMigrationTypeFromPath(oldPath);

            if (!Directory.Exists(oldPath))
            {
                MigrationProgress.Report(new MigrationProgress
                {
                    FileCountTotal = filesCount,
                    CurrentFile = processedFiles,
                    Status = $"跳过：{currentType}（源目录不存在）",
                    CurrentType = currentType
                });
                continue;
            }

            // 确保目标目录存在
            Directory.CreateDirectory(newPath);

            // 获取所有文件
            var allFiles = Directory.GetFiles(oldPath, "*", SearchOption.AllDirectories);

            MigrationProgress.Report(new MigrationProgress
            {
                FileCountTotal = filesCount,
                CurrentFile = processedFiles,
                Status = $"开始迁移 {currentType} ({allFiles.Length} 个文件)",
                CurrentType = currentType
            });

            // 分批处理文件，避免UI卡顿
            var batchSize = 50;
            for (var i = 0; i < allFiles.Length; i += batchSize)
            {
                var batchFiles = allFiles.Skip(i).Take(batchSize).ToList();

                // 异步处理一批文件
                await Task.Run(() =>
                {
                    foreach (var file in batchFiles)
                        try
                        {
                            var relativePath = Path.GetRelativePath(oldPath, file);
                            var targetPath = Path.Combine(newPath, relativePath);

                            var targetDirectory = Path.GetDirectoryName(targetPath);
                            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);

                            File.Copy(file, targetPath, true);

                            processedFiles++;

                            // 每批报告一次进度，减少UI更新
                            if (processedFiles % 10 == 0 || processedFiles == filesCount)
                                MigrationProgress.Report(new MigrationProgress
                                {
                                    FileCountTotal = filesCount,
                                    CurrentFile = processedFiles,
                                    Status = $"正在迁移 {currentType}: {Path.GetFileName(file)}",
                                    CurrentType = currentType,
                                    Percentage = (double)processedFiles / filesCount * 100
                                });
                        }
                        catch (Exception ex)
                        {
                            // 记录错误
                            Console.WriteLine($"文件复制失败: {file}, 错误: {ex.Message}");
                        }
                });
            }

            MigrationProgress.Report(new MigrationProgress
            {
                FileCountTotal = filesCount,
                CurrentFile = processedFiles,
                Status = $"{currentType} 迁移完成",
                CurrentType = currentType,
                Percentage = (double)processedFiles / filesCount * 100
            });
        }

        // 完成
        MigrationProgress.Report(new MigrationProgress
        {
            FileCountTotal = filesCount,
            CurrentFile = processedFiles,
            Status = "所有迁移完成",
            IsCompleted = true,
            Percentage = 100
        });
    }

    private string GetMigrationTypeFromPath(string path)
    {
        if (path.Contains("minecraftWorlds")) return "存档";
        if (path.Contains("minecraftpe")) return "配置";
        if (path.Contains("resource_packs")) return "资源包";
        if (path.Contains("behavior_packs")) return "行为包";
        return "其他";
    }
}