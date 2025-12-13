using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.CornerSelectBar;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public static MainPage Instance;
    public bool IsEditMode { get; set; } = false;

    public MainPage()
    {
        InitializeComponent();

        Instance = this;

        IsEditMode = true;
        SelTag_OnSelectionChanged(null, null);

        if (GlobalModel.Config.Data.IsAutoCheckUpdate) Update();
    }

    public static async Task Update(bool isShowNeo = false)
    {
        try
        {
            var result = await CheckUpdate.Update();
            if (result != null)
                DialogHost.Show(new DialogInfo()
                {
                    Content = $"我们有新的更新：\n\n{result.Body}",
                    Title = $"更新 {result.TagName}",
                    CloseButtonText = "现在更新",
                    PrimaryButtonText = "取消",
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            else if(isShowNeo)
                DialogHost.Show(new DialogInfo()
                {
                    Content = $"当前已是最新版本",
                    Title = $"检查更新",
                    CloseButtonText = "确定"
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新失败：{ex.Message}");
        }
    }

    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var item = (CornerSelectBarItem)SelTag.SelectedItem;
            var tag = item.Tag as string;

            BedrockBootPage page = null;

            switch (tag)
            {
                case "Home":
                    page = new MainHomePage();
                    break;
                case "Manager":
                    page = new MainManager();
                    break;
                case "Download":
                    page = new MainDownloadPage();
                    break;
                case "Task":
                    page = new MainTaskPage();
                    break;
                case "ToolsBox":
                    page = new MainToolsBoxPage();
                    break;
                case "Setting":
                    page = new MainSettingPage();
                    break;
            }

            if (page.HeaderView != null)
            {
                HeaderContent.NavigateTo(page.HeaderView);
            }
            
            MainFrame.NavigateTo(page);
        }
    }
}