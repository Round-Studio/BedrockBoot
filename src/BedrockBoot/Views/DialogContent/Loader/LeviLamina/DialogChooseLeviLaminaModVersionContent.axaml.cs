using System.Collections.Generic;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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