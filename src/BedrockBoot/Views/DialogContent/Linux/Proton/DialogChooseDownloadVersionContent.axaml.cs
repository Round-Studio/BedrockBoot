using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Proton.Enum;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Linux.Proton;

public partial class DialogChooseDownloadVersionContent : UserControl
{
    private readonly ProtonSource _type;
    private List<ProtonInfo>? _lst;

    public ProtonInfo ProtonInfo => _lst![SelBox.SelectedIndex];

    public DialogChooseDownloadVersionContent()
    {
        InitializeComponent();
    }
    
    public DialogChooseDownloadVersionContent(ProtonSource type):this()
    {
        _type = type;

        UpdateUI();
    }
    
    public async Task UpdateUI()
    {
        _lst = (await ProtonCore.GetInstallableVersion(_type))!.ToList();
        if (_lst == null || _lst.Count == 0)
        {
            DialogHost.Close();
            DialogHost.Show(new DialogInfo()
            {
                Title = "出现错误",
                Content = "您的网络连接可能出现错误，我们无法获取到此分支的所有版本。",
                CloseButtonText = "确定"
            });
        }
        
        _lst?.ToList().Select(x => x.Name).ToList()
            .ForEach(x => SelBox.Items.Add(new ComboBoxItem() { Content = x }));
        SelBox.SelectedIndex = 0;
        LoadRing.IsVisible = false;
        SelBox.IsVisible = true;
    }
}