using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Helper;
using Round.SDK.Entity;

namespace BedrockBoot.Win32;

public partial class LaunchWindow : Form
{
    private VersionConfig VersionInfo;
    private EasyLauncher _launcher;
    
    public LaunchWindow()
    {
        InitializeComponent();
    }

    public LaunchWindow(List<string> args) : this()
    {
        try
        {
            var folder = args[args.FindIndex(a => a == "-jump") + 1];
            if (!Directory.Exists(folder))  // 使用 Directory.Exists 而不是 Path.Exists
                throw new Exception("The path doesn't exist");
            
            VersionInfo = GameInfoHelper.GetVersionConfig(folder);
            
            // 确保在 UI 线程上更新控件
            if (GameNameBox.InvokeRequired)
            {
                GameNameBox.Invoke(new Action(() => GameNameBox.Text = VersionInfo.Info.VersionName));
            }
            else
            {
                GameNameBox.Text = VersionInfo.Info.VersionName;
            }
        }
        catch
        {
            MessageBox.Show("无效版本", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
    
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // 在窗体加载完成后启动游戏
        Task.Run(Launch);
    }

    public async Task Launch()
    {
        try
        {
            _launcher = new EasyLauncher(VersionInfo);

            // 设置迁移回调
            _launcher.OnMigration = () =>
            {
                // 使用 Invoke 来确保在 UI 线程上显示消息框
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("该版本不支持快捷启动，请进入启动器 UI 进行迁移", 
                            "需要迁移", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }));
                }
                else
                {
                    MessageBox.Show("该版本不支持快捷启动，请进入启动器 UI 进行迁移", 
                        "需要迁移", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            };

            // 设置进度更新回调
            _launcher.UpdateProgress = (status, percentage) =>
            {
                // 使用 BeginInvoke 避免阻塞
                this.BeginInvoke(new Action(() =>
                {
                    ProgressBox.Text = $"{status} ({percentage:F0}%)";
                    LaunchProgressBar.Value = (int)percentage;
                }));
            };

            // 设置进度文本更新回调
            _launcher.UpdateProgressText = (text) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    ProgressBox.Text = text;
                }));
            };

            // 设置进度条模式回调
            _launcher.SetProgressIndeterminate = (isIndeterminate) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    LaunchProgressBar.Style = isIndeterminate ? 
                        ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                    if (!isIndeterminate)
                    {
                        LaunchProgressBar.Style = ProgressBarStyle.Continuous;
                    }
                }));
            };

            // 设置启动完成回调
            _launcher.LaunchCompleted = () =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.Close();
                }));
            };

            // 设置游戏启动回调
            _launcher.Launched = (process) =>
            {
                Console.WriteLine($"游戏已启动，进程ID: {process.Id}");
                // 可以更新 UI 显示游戏已启动
                this.BeginInvoke(new Action(() =>
                {
                    ProgressBox.Text = "游戏已启动";
                }));
            };

            // 启动游戏
            await _launcher.Launch();
        }
        catch (Exception ex)
        {
            // 异常处理
            this.BeginInvoke(new Action(() =>
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }));
        }
    }
}