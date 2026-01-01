using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Windows.Shapes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Transformation;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceMods : ISetting
{
    public VersionConfig VersionInfo { get; set; }
    public ModsManager ModsManager { get; set; }
    private string _searchKey => SearchBox.Text;
    public InstanceMods()
    {
        IsEdit = false;
        
        InitializeComponent();
    }

    public InstanceMods(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        ModsManager = new(VersionInfo)
        {
            RefreshCallBack = UpdateUI
        };
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        IsEdit = false;
        NullBox.IsVisible = false;
        ResultBox.Children.Clear();
        var mods = ModsManager.RefreshMods();
        var resultMods = new List<ModInfo>();

        mods.ForEach(info =>
        {
            if (string.IsNullOrEmpty(_searchKey) ||
                info.File.Contains(_searchKey))
            {
                resultMods.Add(info);
            }
        });

        if (resultMods.Count <= 0)
        {
            NullBox.IsVisible = true;
        }
        else
        {
            resultMods.ForEach(info =>
            {
                ResultBox.Children.Add(new GameModItem(info)
                {
                    ModsManager = this.ModsManager
                });
            });
        }

        IsEdit = true;
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
            UpdateUI();
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("explorer", new[] { Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods") });
    }

    private void ImportModBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportModContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "添加 Mod 文件",
            Content = dialog,
            CloseButtonText = "添加",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.ModFile) || 
                    !File.Exists(dialog.ModFile))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                    {
                        Message = "无效路径，无法添加模组",
                        Title = "无效路径"
                    });
                    return;
                }

                var path = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods",
                    Path.GetFileName(dialog.ModFile));
                File.Copy(dialog.ModFile, path);
                
                ModsManager.AddMod(new ModInfo()
                {
                    File = path,
                    InjectDelay = dialog.ModDelay
                });
                UpdateUI();
            }
        });
    }
}