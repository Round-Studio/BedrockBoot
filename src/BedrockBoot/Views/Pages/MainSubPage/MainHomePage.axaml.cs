using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public MainHomePage()
    {
        InitializeComponent();
    }
}