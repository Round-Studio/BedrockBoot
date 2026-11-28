/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Net.Http;
using BedrockBoot.Models.Account.Github;
using Octokit;

namespace BedrockBoot.Models.Helper;

public static class GitHubHelper
{
    public static GitHubError HandleException(Exception exception)
    {
        // 刷新凭证被撤销/过期：本地令牌已清空，只能重新授权
        if (exception is GitHubReauthRequiredException reauthRequired)
        {
            return new GitHubError(
                GitHubErrorType.ReauthRequired,
                reauthRequired.Message);
        }

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
        var key = Type switch
        {
            GitHubErrorType.HttpRequestException => "GitHub.Error.HttpRequest",
            GitHubErrorType.RateLimit => "GitHub.Error.RateLimit",
            GitHubErrorType.ReauthRequired => "GitHub.Error.ReauthRequired",
            GitHubErrorType.Unauthorized => "GitHub.Error.Unauthorized",
            GitHubErrorType.Forbidden => "GitHub.Error.Forbidden",
            GitHubErrorType.NotFound => "GitHub.Error.NotFound",
            GitHubErrorType.ApiError => "GitHub.Error.ApiError",
            _ => "GitHub.Error.Unknown"
        };

        return I18nManager.Instance[key];
    }
}

public enum GitHubErrorType
{
    RateLimit,
    Forbidden,
    Unauthorized,
    ReauthRequired,
    NotFound,
    HttpRequestException,
    ApiError,
    Unknown
}
