using Avalonia.Controls;
using Octokit;

namespace BedrockBoot.Views.Control.Items;

public partial class ContributorItem : UserControl
{
    public ContributorItem(RepositoryContributor con)
    {
        InitializeComponent();

        NameText.Text = con.Login;
        ContributorText.Text = $"{con.Contributions}";
        IconBox.Update(con.AvatarUrl);
    }
}