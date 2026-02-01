using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupRoot : UserControl
{
    public Dictionary<string, object> PageDictionary = new()
    {
        ["欢迎"] = new SetupWelcome(),
        ["个性化"] = new SetupStyle(),
        ["导入"] = new SetupImport(),
        ["完成"] = new SetupCompleted()
    };

    public int StepIndex;

    public SetupRoot()
    {
        InitializeComponent();

        PageDictionary.ToList().ForEach(x =>
        {
            TopProgressBar.Items.Add(new TabItem
            {
                Header = x.Key
            });
        });
        UpdatePage();
        UpdateButton();
    }

    private void UpdateButton()
    {
        if (StepIndex == 0)
        {
            ButtonNext.IsEnabled = true;
            ButtonLast.IsEnabled = false;
        }
        else if (StepIndex >= PageDictionary.Count - 1)
        {
            ButtonNext.IsEnabled = false;
            ButtonLast.IsEnabled = true;
        }
        else
        {
            ButtonNext.IsEnabled = true;
            ButtonLast.IsEnabled = true;
        }
    }

    private void UpdatePage()
    {
        SetupFrame.NavigateTo(PageDictionary.ToList()[StepIndex].Value);
        TopProgressBar.SelectedIndex = StepIndex;
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        StepIndex++;
        UpdateButton();
        UpdatePage();
    }

    private void ButtonLast_OnClick(object? sender, RoutedEventArgs e)
    {
        StepIndex--;
        UpdateButton();
        UpdatePage();
    }
}