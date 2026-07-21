using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Isolation;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogChooseInstanceUserContent : UserControl
{
    private readonly VersionConfig? _config;
    private readonly List<string> users;

    public string ChooseUser => users[InstanceUsers.SelectedIndex];

    public DialogChooseInstanceUserContent()
    {
        InitializeComponent();
    }

    public DialogChooseInstanceUserContent(VersionConfig config) : this()
    {
        _config = config;
        users = IsolationCore.GetInstanceUsers(config);
        InstanceUsers.ItemsSource = users;
        InstanceUsers.SelectedIndex = 0;
    }
}