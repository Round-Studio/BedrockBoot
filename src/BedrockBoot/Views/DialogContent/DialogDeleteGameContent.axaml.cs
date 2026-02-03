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
    public DialogDeleteGameContent()
    {
        InitializeComponent();
    }

    public DialogDeleteGameContent(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        Delete();
    }

    public VersionConfig VersionInfo { get; set; }

    public void Delete()
    {
        Task.Run(() =>
        {
            var path = VersionInfo.VersionPath;
            Console.WriteLine($@"即将删除文件夹：{path}");

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            Console.WriteLine($@"总数目：{files.Length}");
            Dispatcher.UIThread.Invoke(() => DeleteProgressBar.Maximum = files.Length);

            Dispatcher.UIThread.Invoke(() => DeleteProgressBar.IsIndeterminate = false);

            var jd = 0;
            files.ToList().ForEach(file =>
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
                if (jd % 200 == 0)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DeleteProgressText.Text = $"进度：{jd * 100.0 / files.Length:F2} %";
                        DeleteProgressBar.Value = (int)jd;
                    });
            });

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
            }

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            Dispatcher.UIThread.Invoke(GlobalModel.MainWindow.CloseDraw);
        });
    }
}