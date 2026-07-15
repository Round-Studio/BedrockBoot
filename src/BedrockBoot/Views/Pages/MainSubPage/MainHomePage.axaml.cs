using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Style.Widgets;
using BedrockBoot.Views.Control.Widgets;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public DesktopWorkspace DesktopWorkspace;
    public MainHomePage()
    {
        InitializeComponent();
        UpdateHome();
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();
        DesktopWorkspace = new DesktopWorkspace();
        if (File.Exists(PathsList.WidgetsConfigPath))
            DesktopWorkspace.ImportLayout(File.ReadAllText(PathsList.WidgetsConfigPath));
        DesktopWorkspace.LayoutChanged += (sender, args) =>
        {
            var json = DesktopWorkspace.ExportLayout();
            File.WriteAllText(PathsList.WidgetsConfigPath, json);
        };

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
            case HomeType.Widgets:
                MainGrid.Children.Add(DesktopWorkspace);
                break;
        }
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        DesktopWorkspace.AddWidget(new DesktopWidgetTemplated());
    }
}