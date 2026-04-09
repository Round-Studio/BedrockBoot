using System;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Loader;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public MainHomePage()
    {
        InitializeComponent();
        UpdateHome();
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();
        switch (BedrockBoot.Core.Global.GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
        }
    }
}