using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDeleteGameContent : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public DialogDeleteGameContent()
    {
        InitializeComponent();
    }
    public DialogDeleteGameContent(VersionConfig versionInfo):this()
    {
        VersionInfo = versionInfo;

        Delete();
    }

    public async Task Delete()
    {
        var path = VersionInfo.VersionPath;
        Console.WriteLine($"即将删除文件夹：{path}");
        
        var files = Directory.GetFiles(path,"*", SearchOption.AllDirectories);
        Console.WriteLine($"总数目：{files.Length}");
        DeleteProgressBar.Maximum = files.Length;

        await Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() => DeleteProgressBar.IsIndeterminate = false);
            
            var jd = 0;
            files.ToList().ForEach((file) =>
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
                jd++;
                if (jd % 20 == 0)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DeleteProgressText.Text = $"进度：{(jd * 100.0 / files.Length):F2} %";
                        DeleteProgressBar.Value = jd;
                    });
                }
            });
            
            Directory.Delete(path, true);

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            Dispatcher.UIThread.Invoke(GlobalModel.MainWindow.CloseDraw);
        });
    }
}