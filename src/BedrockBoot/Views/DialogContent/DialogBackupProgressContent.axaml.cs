using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogBackupProgressContent : UserControl
{
    private readonly ArchiveInfo _info;
    private readonly string _backupName;
    private readonly Action _success;

    public DialogBackupProgressContent()
    {
        InitializeComponent();
    }
    
    public DialogBackupProgressContent(ArchiveInfo info,string backupName,Action success):this()
    {
        _info = info;
        _backupName = backupName;
        _success = success;
        Backup();
    }

    public async Task Backup()
    {
        await GlobalModel.ArchiveBackup.BackupAsync(_info, _backupName, new Progress<string>((s) =>
        {
            Console.WriteLine($@"备份进度：{s}");
            Dispatcher.UIThread.Invoke(() => { ProgressText.Text = $"备份进度：{s} %"; });
        }));

        DialogHost.Close();
        _success.Invoke();
    }
}