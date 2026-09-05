using System;
using System.Net;
using System.Net.Http;
using Octokit;

namespace BedrockBoot.Helpers;

public static class GitHubHelper
{
    public static GitHubError HandleException(Exception exception)
    {
        if (exception is ApiException apiException)
        {
            if (apiException.StatusCode is
                HttpStatusCode.Forbidden or
                HttpStatusCode.TooManyRequests)
            {
                if (apiException.Message.Contains(
                        "rate limit",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new GitHubError(
                        GitHubErrorType.RateLimit,
                        apiException.Message);
                }

                return new GitHubError(
                    GitHubErrorType.Forbidden,
                    apiException.Message);
            }

            return apiException.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new(
                    GitHubErrorType.Unauthorized,
                    apiException.Message),

                HttpStatusCode.NotFound => new(
                    GitHubErrorType.NotFound,
                    apiException.Message),

                _ => new(
                    GitHubErrorType.ApiError,
                    apiException.Message)
            };
        }

        if (exception is HttpRequestException)
        {
            return new GitHubError(
                GitHubErrorType.HttpRequestException,
                exception.Message);
        }

        return new GitHubError(
            GitHubErrorType.Unknown,
            exception.Message);
    }
}

public sealed record GitHubError(
    GitHubErrorType Type,
    string Message)
{
    public string GetLocalizedMessage()
    {
        return Type switch
        {
            GitHubErrorType.HttpRequestException =>
                "Http Request Exception",

            GitHubErrorType.RateLimit =>
                "GitHub API rate limit exceeded for your IP", 

            GitHubErrorType.Unauthorized =>
                "Unauthorized",

            GitHubErrorType.Forbidden =>
                "Forbidden",

            GitHubErrorType.NotFound =>
                "Not Found",

            GitHubErrorType.ApiError =>
                "API Error",

            _ =>
                "Unknown Error"
        };
    }
}

public enum GitHubErrorType
{
    RateLimit,
    Forbidden,
    Unauthorized,
    NotFound,
    HttpRequestException,
    ApiError,
    Unknown
}
