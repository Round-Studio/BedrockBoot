using System.Collections.Generic;
using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent.Plugin.LeviLamina;

public partial class DialogLeviLaminaChooseVersionContent : UserControl
{
    private readonly List<string> _versions;

    public DialogLeviLaminaChooseVersionContent()
    {
        InitializeComponent();
    }

    public DialogLeviLaminaChooseVersionContent(List<string> versions) : this()
    {
        _versions = versions;
        versions.ForEach(v => VersionList.Items.Add(new ListBoxItem
        {
            Content = v
        }));
        VersionList.SelectedIndex = 0;
    }

    public string Version => _versions[VersionList.SelectedIndex];
}