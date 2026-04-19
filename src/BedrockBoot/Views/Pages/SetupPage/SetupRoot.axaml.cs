using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

// 确保引用了 I18nManager 所在的命名空间

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupRoot : UserControl
{
    // 将 Key 改为内部标识符，Value 对应页面实例
    public Dictionary<string, object> PageDictionary = new()
    {
        ["Setup.Step.Welcome"] = new SetupWelcome(),
        ["Setup.Step.Style"] = new SetupStyle(),
        ["Setup.Step.Import"] = new SetupImport(),
        ["Setup.Step.Completed"] = new SetupCompleted()
    };

    public int StepIndex;

    public SetupRoot()
    {
        InitializeComponent();

        // 初始化顶部的步骤条
        foreach (var x in PageDictionary)
            TopProgressBar.Items.Add(new TabItem
            {
                // 从 I18nManager 获取翻译后的文本作为 Header
                Header = I18nManager.Instance[x.Key]
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
        // 使用 ElementAt 访问字典中的 Value
        SetupFrame.NavigateTo(PageDictionary.ElementAt(StepIndex).Value);
        TopProgressBar.SelectedIndex = StepIndex;
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        if (StepIndex < PageDictionary.Count - 1)
        {
            StepIndex++;
            UpdateButton();
            UpdatePage();
        }
    }

    private void ButtonLast_OnClick(object? sender, RoutedEventArgs e)
    {
        if (StepIndex > 0)
        {
            StepIndex--;
            UpdateButton();
            UpdatePage();
        }
    }
}