using BedrockBoot.Models.Global;
using Octokit;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Github;

public static class GitHubApiClient
{
    public static string? GetUserToken()
    {
        var config = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount, false);
        return config.Data?.UserToken;
    }

    public static GitHubClient CreateClient()
    {
        var client = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        var token = GetUserToken();
        if (!string.IsNullOrEmpty(token))
            client.Credentials = new Credentials(token);

        return client;
    }
}
