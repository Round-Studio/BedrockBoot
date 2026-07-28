using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;

namespace BedrockBoot.Models.Account.Microsoft.Helper;

public static class QueryHelpers
{
    public static (string codeVerifier, string codeChallenge) GeneratePkceCodes()
    {
        byte[] bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        string codeVerifier = Base64UrlEncode(bytes);

        using (var sha256 = SHA256.Create())
        {
            byte[] challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            string codeChallenge = Base64UrlEncode(challengeBytes);
            return (codeVerifier, codeChallenge);
        }
    }

    public static string Base64UrlEncode(byte[] input)
    {
        string base64 = Convert.ToBase64String(input);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static NameValueCollection ParseQueryString(string query)
    {
        var result = new NameValueCollection();
        if (string.IsNullOrEmpty(query) || query[0] != '?')
            return result;

        string[] pairs = query.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=', 2);
            if (kv.Length == 2)
            {
                string key = Uri.UnescapeDataString(kv[0]);
                string value = Uri.UnescapeDataString(kv[1]);
                result[key] = value;
            }
        }
        return result;
    }
}