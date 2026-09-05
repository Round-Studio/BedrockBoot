using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogChooseLeviLaminaInstallVersionContent : UserControl
{
    private readonly List<string> _versions;
    public string ChooseVersion => _versions[ComboBox.SelectedIndex];

    public DialogChooseLeviLaminaInstallVersionContent()
    {
        InitializeComponent();
    }

    public DialogChooseLeviLaminaInstallVersionContent(List<string> versions) : this()
    {
        _versions = versions;
        ComboBox.ItemsSource = _versions;
        ComboBox.SelectedIndex = _versions.Count - 1;
    }
}