using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawImportOtherLauncherContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    public DrawImportOtherLauncherContent()
    {
        InitializeComponent();
        InitializeLauncherList();
    }

    /// <summary>
    /// 初始化并渲染可导入的启动器列表
    /// </summary>
    private void InitializeLauncherList()
    {
        LaunchersBox.Children.Clear();
        bool anyVisible = false;

        // PathsList.OtherLauncher 定义了支持扫描的启动器列表
        foreach (var launcher in PathsList.OtherLauncher)
        {
            // 如果配置文件存在，或者该启动器标记为强制显示（!IsExists）
            if (File.Exists(launcher.ConfigFile) || !launcher.IsExists)
            {
                anyVisible = true;
                
                var item = new SettingCard
                {
                    IsClickable = true,
                    // 假设框架属性名为 IsFontIcon，设为 false 以显示图片
                    IsFontIcon = false, 
                    ImageIcon = LoadResourceBitmap(launcher.IconUrl),
                    Header = launcher.Name,
                    Description = string.Format(i18n["Import.Launcher.Description.Format"], launcher.Name)
                };

                // 绑定点击事件执行具体的导入逻辑
                item.Click += (sender, args) =>
                {
                    launcher.OnImport?.Invoke(launcher.ConfigFile);
                    // 导入后通常关闭侧边栏
                    GlobalModel.MainWindow.CloseDraw();
                };

                LaunchersBox.Children.Add(item);
            }
        }

        // 如果没有检测到任何启动器，显示“无内容”提示
        NoneBox.IsVisible = !anyVisible;
    }

    /// <summary>
    /// 安全加载资源图片
    /// </summary>
    private Bitmap? LoadResourceBitmap(string url)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(url));
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load icon {url}: {ex.Message}");
            return null;
        }
    }
}