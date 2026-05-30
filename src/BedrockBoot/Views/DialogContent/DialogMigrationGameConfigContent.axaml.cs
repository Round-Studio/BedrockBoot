using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Isolation;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Pack.Game.Isolation;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMigrationGameConfigContent : UserControl
{
    public DialogMigrationGameConfigContent()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     初始化并自动开始迁移任务
    /// </summary>
    /// <param name="conf">迁移配置信息</param>
    public DialogMigrationGameConfigContent(MigrationConfig conf) : this()
    {
        StartMigration(conf);
    }

    private static I18nManager i18n => I18nManager.Instance;

    private void StartMigration(MigrationConfig conf)
    {
        Task.Run(async () =>
        {
            try
            {
                var core = new IsolationMigration
                {
                    MigrationProgress = new Progress<MigrationProgress>(progress =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (MigrationProgressBar.IsIndeterminate)
                                MigrationProgressBar.IsIndeterminate = false;

                            MigrationProgressBar.Value = progress.Percentage;

                            MigrationProgressText.Text =
                                $"{i18n["Instance.Isolation.Migrating"]} ({progress.Percentage:F2} %)";
                        });
                    })
                };

                await core.MigrateFoldersAsync(conf);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
            }
            finally
            {
                Dispatcher.UIThread.Invoke(DialogHost.Close);
            }
        });
    }
}