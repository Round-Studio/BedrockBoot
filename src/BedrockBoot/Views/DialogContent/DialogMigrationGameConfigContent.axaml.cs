using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
    public DialogMigrationGameConfigContent(MigrationConfig conf) : this()
    {
        Task.Run(async () =>
        {
            var core = new IsolationMigration()
            {
                MigrationProgress = new Progress<MigrationProgress>(progress =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (MigrationProgressBar.IsIndeterminate) MigrationProgressBar.IsIndeterminate = false;
                        MigrationProgressBar.Value = progress.Percentage;
                        MigrationProgressText.Text = $"迁移中... ({progress.Percentage:F2} %)";
                    });
                })
            };
            await core.MigrateFoldersAsync(conf);
            Dispatcher.UIThread.Invoke(DialogHost.Close);
        });
    }
}