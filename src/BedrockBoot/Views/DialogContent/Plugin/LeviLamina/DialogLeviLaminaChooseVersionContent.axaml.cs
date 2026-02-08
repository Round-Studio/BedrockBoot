using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot.Views.DialogContent.Plugin.LeviLamina;

public partial class DialogLeviLaminaChooseVersionContent : UserControl
{
    public string Version => _versions[VersionList.SelectedIndex];
    private List<string> _versions;
    public DialogLeviLaminaChooseVersionContent()
    {
        InitializeComponent();
    }

    public DialogLeviLaminaChooseVersionContent(List<string> versions) : this()
    {
        _versions = versions;
        versions.ForEach(v => VersionList.Items.Add(new ListBoxItem()
        {
            Content = v
        }));
        VersionList.SelectedIndex = 0;
    }
}