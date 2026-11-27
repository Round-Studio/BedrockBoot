/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
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
        // 这里还在 UI 线程（构造函数里调进来），先把进度归零并把文案按当前语言渲染好，
        // 免得读大目录的这段时间停在未本地化的占位符上
        if (Dispatcher.UIThread.CheckAccess())
            ReportProgress(0, 1);
        else
            Dispatcher.UIThread.Post(() => ReportProgress(0, 1));

        Task.Run(() =>
        {
            var path = VersionInfo?.VersionPath;

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Console.WriteLine(@"删除实例失败：版本目录为空");
                    return;
                }

                if (!Directory.Exists(path))
                {
                    Console.WriteLine($@"版本目录已不存在，跳过删除：{path}");
                    return;
                }

                Console.WriteLine($@"即将删除文件夹：{path}");

                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                Console.WriteLine($@"总数目：{files.Length}");
                // 拿到文件总数后才能确定进度区间，从这一刻起进度才有意义
                Dispatcher.UIThread.Invoke(() =>
                {
                    DeleteProgressBar.Maximum = files.Length;
                    DeleteProgressBar.Value = 0;
                });

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
                        Dispatcher.UIThread.Invoke(() => ReportProgress(jd, files.Length));
                });

                // 收尾再刷一次：文件数不足 200 时上面那段永远不会触发，进度文字会一直停着
                Dispatcher.UIThread.Invoke(() => ReportProgress(jd, files.Length));

                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"删除版本目录失败：{e.Message}");
                }
            }
            catch (Exception e)
            {
                // 读目录阶段就失败（权限、占用等）时也不能把用户卡在无法关闭的弹窗里
                Console.WriteLine($@"删除实例失败：{e.Message}");
            }
            finally
            {
                // 无论删除是否完全成功都收尾：目录内容已经变了，列表必须重建
                Dispatcher.UIThread.Post(() =>
                {
                    DialogHost.Close();
                    GlobalModel.MainWindow?.CloseDraw();

                    MainManager.RefreshVersionLists();
                });
            }
        });
    }

    /// <summary>刷新进度条与百分比文字，必须在 UI 线程调用。</summary>
    private void ReportProgress(int done, int total)
    {
        if (total <= 0)
        {
            // 空目录也要把条和文字归零，别留上一帧的状态
            DeleteProgressBar.Value = 0;
            UpdateProgressText(0d);
            return;
        }

        DeleteProgressBar.Value = done;
        UpdateProgressText(done * 100.0 / total);
    }

    /// <summary>百分比保留两位小数由 i18n 文案里的 {0:F2} 决定，避免每种语言各拼一次。</summary>
    private void UpdateProgressText(double percent)
    {
        DeleteProgressText.Text = string.Format(I18nManager.Instance["UI.Dialog.DeleteProgress"], percent);
    }
}