using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Models.Pack.Game.Mods;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class GameModItem : UserControl
{
    public GameModItem()
    {
        InitializeComponent();
    }

    public GameModItem(ModInfo info) : this()
    {
        ModInfo = info;

        UpdateUI();
    }

    public ModInfo ModInfo { get; set; }
    public ModsManager ModsManager { get; set; }

    public void UpdateUI()
    {
        FileName.Text = Path.GetFileName(ModInfo.File);

        if (!ModInfo.IsPreLoad)
            Card.Description = $"{SizeHelper.FormatBytes(new FileInfo(ModInfo.File).Length)}，{ModInfo.InjectDelay} ms";
        else
            Card.Description = $"{SizeHelper.FormatBytes(new FileInfo(ModInfo.File).Length)}";

        PreLoadBox.IsVisible = ModInfo.IsPreLoad;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "删除模组",
            Content = $"您确定要删除模组 {Path.GetFileName(ModInfo.File)} 吗\n" +
                      $"这将永远无法恢复。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                try
                {
                    File.Delete(ModInfo.File);
                    ModsManager.RefreshMods(true);
                }
                catch (Exception e)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "出现错误",
                        Content = $"删除模组 {Path.GetFileName(ModInfo.File)} 时\n" +
                                  $"出现错误：{e.Message}",
                        CloseButtonText = "确定"
                    });
                }
            }
        });
    }
}