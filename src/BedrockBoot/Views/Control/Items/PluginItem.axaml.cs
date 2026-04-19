using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPluginPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entry;

namespace BedrockBoot.Views.Control.Items;

public partial class PluginItem : UserControl
{
    private readonly PackConfig _info;
    private readonly bool _isInitializing = true;

    public PluginItem()
    {
        InitializeComponent();
    }

    public PluginItem(PackConfig info) : this()
    {
        _info = info;
        UpdateUI();

        // 设置初始开关状态
        EnableSwitch.IsChecked = _info.IsEnable;
        _isInitializing = false;

        // 绑定切换事件
        EnableSwitch.IsCheckedChanged += (s, e) =>
        {
            if (_isInitializing) return;

            var isChecked = EnableSwitch.IsChecked ?? false;

            // 执行文件重命名逻辑
            PluginLoader.TogglePlugin(_info, isChecked);
        };

        DeleteButton.Click += (s, e) =>
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "删除插件",
                Content = "您确定要删除此插件吗？\n" +
                          $"插件：{info.PackName}",
                CloseButtonText = "确定",
                PrimaryButtonText = "取消",
                AccountButton = DialogButtons.CloseButton,
                CloseAction = () =>
                {
                    if (PluginLoader.Delete(_info))
                    {
                        var manager = this.FindAncestorOfType<PluginManager>();
                        manager?.UpdateUI();
                    }
                }
            });
        };
    }

    public void UpdateUI()
    {
        NameBox.Text = _info.PackName;
        DescriptionBox.Text = _info.PackDescription;
        VersionBox.Text = _info.PackVersion;

        // 处理图标
        if (!string.IsNullOrEmpty(_info.PackIconPath) && File.Exists(_info.PackIconPath))
            try
            {
                Card.ImageIcon = new Bitmap(_info.PackIconPath);
                Card.IsFontIcon = false;
            }
            catch
            {
                /* 忽略损坏的图片 */
            }
    }
}