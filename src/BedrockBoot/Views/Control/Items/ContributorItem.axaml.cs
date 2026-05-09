using System;
using Avalonia.Controls;
using Octokit;

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
    }
}