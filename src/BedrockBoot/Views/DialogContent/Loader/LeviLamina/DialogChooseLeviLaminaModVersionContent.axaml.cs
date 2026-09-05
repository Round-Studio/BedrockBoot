using System.Collections.Generic;
using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogChooseLeviLaminaModVersionContent : UserControl
{
    private readonly List<string> _versions;
    public string ChooseVersion => _versions[ComboBox.SelectedIndex];

    public DialogChooseLeviLaminaModVersionContent()
    {
        InitializeComponent();
    }

    public DialogChooseLeviLaminaModVersionContent(List<string> versions) : this()
    {
        _versions = versions;
        ComboBox.ItemsSource = versions;
        ComboBox.SelectedIndex = versions.Count - 1;
    }
}