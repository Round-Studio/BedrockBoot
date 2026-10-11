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
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity;
using OnePointUI.Avalonia.Style.Core;

namespace BedrockBoot.Models.Account.Github;

public class GitHubOAuthManager
{
    private string _clientId => BedrockBoot.Core.Keys.GithubClientId;
    private string _clientSecret => BedrockBoot.Core.Keys.GithubSecret;

    private static int CallbackPort = (new Random()).Next(10000, 49152);
    private const string CallbackPath = "/auth/github/callback";
    private string RedirectUri => $"http://127.0.0.1:{CallbackPort}{CallbackPath}";

    public async Task<string> LoginAsync()
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{CallbackPort}/");
        listener.Start();

        var state = Guid.NewGuid().ToString("N");
        var authUrl = $"https://github.com/login/oauth/authorize" +
                      $"?client_id={_clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                      $"&scope={Uri.EscapeDataString("user:email")}" +
                      $"&state={state}";

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var context = await listener.GetContextAsync();
        var query = context.Request.QueryString;

        var code = query["code"];
        var returnedState = query["state"];

        var responseHtml = await new JsonResourceEntity()
            .ReadTextResourceAsync("avares://BedrockBoot/Assets/Web/LoginFinish.html");

        // 获取颜色值
        var colors = await GetColorsOnUIThreadAsync();

        responseHtml = responseHtml
            .Replace("{BackgroundBrush}", colors.BackgroundBrush)
            .Replace("{PrimaryForegroundBrush}", colors.PrimaryForegroundBrush)
            .Replace("{PrimaryDisabledForegroundBrush}", colors.PrimaryDisabledForegroundBrush)
            .Replace("{PrimaryDisabled2ForegroundBrush}", colors.PrimaryDisabled2ForegroundBrush)
            .Replace("{PrimaryBorderBrush}", colors.PrimaryBorderBrush)
            .Replace("{AccentBorderBrush}", colors.AccentBorderBrush)
            .Replace("{AccentBackgroundBrush}", colors.AccentBackgroundBrush)
            .Replace("{AccentBackgroundOverBrush}", colors.AccentBackgroundOverBrush);

        var html = responseHtml;
        var buffer = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();

        listener.Stop();

        if (string.IsNullOrEmpty(code) || returnedState != state)
            throw new InvalidOperationException("Invalid callback.");

        return await ExchangeCodeForTokenAsync(code);
    }

    private async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        using var http = new System.Net.Http.HttpClient();
        var body = new System.Net.Http.FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("client_id", _clientId),
            new System.Collections.Generic.KeyValuePair<string, string>("client_secret", _clientSecret),
            new System.Collections.Generic.KeyValuePair<string, string>("code", code),
            new System.Collections.Generic.KeyValuePair<string, string>("redirect_uri", RedirectUri),
        });

        http.DefaultRequestHeaders.Add("Accept", "application/json");
        var resp = await http.PostAsync("https://github.com/login/oauth/access_token", body);
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("access_token", out var token))
            return token.GetString();

        throw new InvalidOperationException($"Token exchange failed: {json}");
    }

    private ColorValues GetColors()
    {
        var colors = new ColorValues
        {
            BackgroundBrush = GetColor("BackgroundBrush"),
            PrimaryForegroundBrush = GetColor("PrimaryForegroundBrush"),
            PrimaryDisabledForegroundBrush = GetColor("PrimaryDisabledForegroundBrush"),
            PrimaryDisabled2ForegroundBrush = GetColor("PrimaryDisabled2ForegroundBrush"),
            PrimaryBorderBrush = GetColor("PrimaryBorderBrush"),
            AccentBorderBrush = GetColor("AccentBorderBrush"),
            AccentBackgroundBrush = GetColor("AccentBackgroundBrush"),
            AccentBackgroundOverBrush = GetColor("AccentBackgroundOverBrush")
        };

        return colors;
    }

    private string GetColor(string key)
    {
        try
        {
            if (App.Current?.Resources == null)
                return "#000000";

            if (App.Current.Resources.TryGetValue(key, out var brush) && brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            var darkTheme = App.Current.Resources.TryGetValue("DarkTheme", out var darkThemeDict)
                ? darkThemeDict as ResourceDictionary
                : null;
            var lightTheme = App.Current.Resources.TryGetValue("LightTheme", out var lightThemeDict)
                ? lightThemeDict as ResourceDictionary
                : null;

            var currentTheme = ThemeManager.Instance?.CurrentTheme;
            var targetDict = currentTheme == ThemeVariant.Dark ? darkTheme : lightTheme;

            if (targetDict != null && targetDict.TryGetValue(key, out var themeBrush) &&
                themeBrush is SolidColorBrush themeSolidBrush)
            {
                var color = themeSolidBrush.Color;
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetColor error for key '{key}': {ex.Message}");
        }

        return "#000000";
    }

    private async Task<ColorValues> GetColorsOnUIThreadAsync()
    {
        var tcs = new TaskCompletionSource<ColorValues>();

        if (Dispatcher.UIThread.CheckAccess())
        {
            var colors = GetColors();
            return colors;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var colors = GetColors();
                tcs.SetResult(colors);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }
}

internal class ColorValues
{
    public string BackgroundBrush { get; set; } = "#000000";
    public string PrimaryForegroundBrush { get; set; } = "#000000";
    public string PrimaryDisabledForegroundBrush { get; set; } = "#000000";
    public string PrimaryDisabled2ForegroundBrush { get; set; } = "#000000";
    public string PrimaryBorderBrush { get; set; } = "#000000";
    public string AccentBorderBrush { get; set; } = "#000000";
    public string AccentBackgroundBrush { get; set; } = "#000000";
    public string AccentBackgroundOverBrush { get; set; } = "#000000";
}