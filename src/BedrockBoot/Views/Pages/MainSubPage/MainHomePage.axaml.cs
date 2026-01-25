using System;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Loader;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public MainHomePage()
    {
        InitializeComponent();

        try
        {
            MainGrid.Children.Add(DynamicLayoutLoader.LoadXamlFromFile(
                GlobalModel.Config.Data.HomeConfig.HomeXmlFiles[GlobalModel.Config.Data.HomeConfig.HomeXmlSelIndex]));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}