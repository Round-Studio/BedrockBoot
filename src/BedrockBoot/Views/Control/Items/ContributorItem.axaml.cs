using System;
using Avalonia.Controls;
using Octokit;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class ContributorItem : UserControl
{
    public ContributorItem()
    {
        InitializeComponent();
    }
    
    public ContributorItem(RepositoryContributor con):this()
    {
        NameText.Content = con.Login;
        NameText.NavigateUri = new Uri(con.HtmlUrl);
        ContributorText.Text = $"{con.Contributions}";
        IconBox.Update(con.AvatarUrl);

        if (con.Login == "Chlna6666")
        {
            NameText.Click += (_,_)=>
            {
                DialogHost.Show(new()
                {
                    Title = "magijj",
                    Content = "?! 蛆蛆 !?",
                    CloseButtonText = "蛆"
                });
            };
        }
    }
}