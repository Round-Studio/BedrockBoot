using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Account.Xbox;
public class XblAuth
{
    readonly HttpClient _http = new();

    static byte[] DerToRaw(byte[] derSig)
    {
        // DER: 30 <seq_len> 02 <r_len> <r> 02 <s_len> <s>
        int offset = 2;
        if (derSig[1] >= 0x80)
        {
            int lenBytes = derSig[1] & 0x7F;
            offset = 2 + lenBytes;
        }
        if (offset >= derSig.Length || derSig[offset] != 0x02)
        {
            // Try IEEE P1363 raw format (64 bytes: r||s)
            if (derSig.Length == 64)
            {
                return derSig;
            }
            throw new InvalidOperationException($"Expected 0x02 tag at offset {offset}, DER: {Convert.ToHexString(derSig)}");
        }
        int rLen = derSig[offset + 1];
        int rStart = offset + 2;
        int rSkip = rLen > 32 ? rLen - 32 : 0;
        int sOffset = rStart + rLen;
        if (sOffset >= derSig.Length || derSig[sOffset] != 0x02)
            throw new InvalidOperationException($"Expected 0x02 tag for s at offset {sOffset}");
        int sLen = derSig[sOffset + 1];
        int sStart = sOffset + 2;
        int sSkip = sLen > 32 ? sLen - 32 : 0;
        var raw = new byte[64];
        Buffer.BlockCopy(derSig, rStart + rSkip, raw, 0, 32 - rSkip);
        Buffer.BlockCopy(derSig, sStart + sSkip, raw, 32, 32 - sSkip);
        return raw;
    }

    string SignHeader(ECDsa key, string method, string path, byte[] bodyBytes)
    {
        var nowFt = (long)((DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 11644473600) * 1e7);
        var ver = BitConverter.GetBytes(1); Array.Reverse(ver);
        var ts = BitConverter.GetBytes(nowFt); Array.Reverse(ts);
        using var ms = new MemoryStream();
        ms.Write(ver); ms.WriteByte(0);
        ms.Write(ts); ms.WriteByte(0);
        ms.Write(Encoding.UTF8.GetBytes(method)); ms.WriteByte(0);
        ms.Write(Encoding.UTF8.GetBytes(path)); ms.WriteByte(0);
        ms.WriteByte(0); // empty auth
        ms.Write(bodyBytes); ms.WriteByte(0);
        var hashInput = ms.ToArray();
        var derSig = key.SignData(hashInput, HashAlgorithmName.SHA256);
        var rawSig = DerToRaw(derSig);
        var sig = new byte[76];
        ver.CopyTo(sig, 0);
        ts.CopyTo(sig, 4);
        rawSig.CopyTo(sig, 12);
        return Convert.ToBase64String(sig);
    }

    HttpResponseMessage? DoPost(string url, object body, ECDsa? signingKey = null)
    {
        var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body);
        var path = new Uri(url).AbsolutePath;
        var key = signingKey ?? DeviceIdentity.LoadOrCreateKey();
        var sig = SignHeader(key, "POST", path, bodyBytes);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(bodyBytes) { Headers = { ContentType = new("application/json") } }
        };
        req.Headers.TryAddWithoutValidation("User-Agent", "XAL Xbox Live Game (Windows; SDK; 1.0.0.0)");
        req.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");
        req.Headers.TryAddWithoutValidation("Signature", sig);
        try
        {
            var resp = _http.Send(req);
            Console.WriteLine($"HTTP {url} -> {resp.StatusCode}");
            return resp;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HTTP {url} failed: {e.Message}");
            return null;
        }
    }

    JsonDocument? PostJson(string url, object body, ECDsa? signingKey = null)
    {
        var resp = DoPost(url, body, signingKey);
        if (resp == null) return null;
        var bodyStr = resp.Content.ReadAsStringAsync().Result;
        Console.WriteLine($"  Response: {resp.StatusCode} ({bodyStr.Length} bytes)");
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"  Response body: {bodyStr}");
            return null;
        }
        try { return JsonDocument.Parse(bodyStr); }
        catch (Exception e) { Console.WriteLine($"  JSON parse failed: {e.Message}"); return null; }
    }

    public bool RunPreauth(string msaAccessToken, string expectedAccountEpoch = "legacy")
    {
        Console.WriteLine("Starting Xbox Live pre-auth...");
        var cache = PathsList.PreauthDir;
        Directory.CreateDirectory(cache);
        try { File.SetUnixFileMode(cache, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); } catch { }

        var deviceId = DeviceIdentity.LoadOrCreateDeviceId();
        using var key = DeviceIdentity.LoadOrCreateKey();
        var pubParams = key.ExportParameters(false);
        var xB64 = Convert.ToBase64String(pubParams.Q.X!);
        var yB64 = Convert.ToBase64String(pubParams.Q.Y!);

        var proofKey = new { alg = "ES256", crv = "P-256", kty = "EC", use = "sig", x = xB64, y = yB64 };
        var rpAuth = "http://auth.xboxlive.com";

        // Device Auth
        var da = PostJson("https://device.auth.xboxlive.com/device/authenticate", new
        {
            RelyingParty = rpAuth, TokenType = "JWT",
            Properties = new { AuthMethod = "ProofOfPossession", Id = deviceId, DeviceType = "Win32", Version = "10.0.22631", ProofKey = proofKey }
        }, key);
        if (da == null) { Console.WriteLine("Device auth failed"); return false; }
        var deviceToken = da.RootElement.GetProperty("Token").GetString()!;
        var deviceTokenExpiry = da.RootElement.TryGetProperty("NotAfter", out var dte) ? dte.GetString() ?? "" : "";
        Console.WriteLine("Device auth OK");

        // User Auth
        string? userToken = null, userTokenExpiry = null;
        if (!string.IsNullOrEmpty(msaAccessToken))
        {
            var ua = PostJson("https://user.auth.xboxlive.com/user/authenticate", new
            {
                RelyingParty = rpAuth, TokenType = "JWT",
                Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = "d=" + msaAccessToken }
            }, key);
            if (ua != null)
            {
                userToken = ua.RootElement.GetProperty("Token").GetString()!;
                userTokenExpiry = ua.RootElement.TryGetProperty("NotAfter", out var ute) ? ute.GetString() ?? "" : "";
                Console.WriteLine("User auth OK");
            }
        }

        // XSTS Achievements
        string? achievementsToken = null, achievementsUhs = null, achievementsExpiry = null;
        if (userToken != null)
        {
            var xsts = PostJson("https://xsts.auth.xboxlive.com/xsts/authorize", new
            {
                RelyingParty = "http://xboxlive.com", TokenType = "JWT",
                Properties = new { SandboxId = "RETAIL", UserTokens = new[] { userToken } }
            }, key);
            if (xsts != null)
            {
                achievementsToken = xsts.RootElement.GetProperty("Token").GetString()!;
                achievementsExpiry = xsts.RootElement.TryGetProperty("NotAfter", out var ae) ? ae.GetString() ?? "" : "";
                try { achievementsUhs = xsts.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString(); } catch { }
                Console.WriteLine("XSTS achievements OK");
            }
        }

        (string? Token, string? Expiry, string? Uhs, string? Rp) DoSisu(string rp, string label)
        {
            if (string.IsNullOrEmpty(msaAccessToken)) return (null, null, null, null);
            var r = PostJson("https://sisu.xboxlive.com/authorize", new
            {
                AccessToken = "t=" + msaAccessToken, AppId = GlobalKeys.MsClientId,
                DeviceToken = deviceToken, Sandbox = "RETAIL", UseModernGamertag = true,
                SiteName = "user.auth.xboxlive.com", RelyingParty = rp,
                OfferTermsAcceptance = true, AcceptOffers = true, ProofKey = proofKey
            }, key);
            if (r == null) { Console.WriteLine($"SISU {label} failed"); return (null, null, null, null); }
            Console.WriteLine($"SISU {label} OK");
            try
            {
                var auth = r.RootElement.GetProperty("AuthorizationToken");
                var token = auth.GetProperty("Token").GetString()!;
                var expiry = auth.TryGetProperty("NotAfter", out var ne) ? ne.GetString() ?? "" : "";
                var uhs = "";
                try { uhs = auth.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() ?? ""; } catch { }
                return (token, expiry, uhs, rp);
            }
            catch { return (null, null, null, null); }
        }

        // SISU Profile
        var (xblToken, xblExpiry, _, _) = DoSisu("http://xboxlive.com", "profile");
        var (sisuToken, sisuExpiry, sisuUhs, sisuRp) = DoSisu("https://b980a380.minecraft.playfabapi.com/", "playfab");
        var (mpToken, mpExpiry, mpUhs, mpRp) = DoSisu("https://multiplayer.minecraft.net/", "multiplayer");
        var (realmsToken, realmsExpiry, realmsUhs, realmsRp) = DoSisu("https://pocket.realms.minecraft.net/", "realms");
        var (licToken, licExpiry, licUhs, licRp) = DoSisu("http://licensing.xboxlive.com", "licensing");

        // Get xuid/gamertag from SISU response (re-fetch for the claims)
        string? xblXuid = null, xblGamertag = null, xblAgeGroup = null, xblUhs = null;
        if (xblToken != null)
        {
            // Parse claims - we need to re-fetch to get DisplayClaims
            var profileSisu = PostJson("https://sisu.xboxlive.com/authorize", new
            {
                AccessToken = "t=" + msaAccessToken, AppId = "e24f843a-df5c-47b9-a407-865d474ccdad",
                DeviceToken = deviceToken, Sandbox = "RETAIL", UseModernGamertag = true,
                SiteName = "user.auth.xboxlive.com", RelyingParty = "http://xboxlive.com",
                OfferTermsAcceptance = true, AcceptOffers = true, ProofKey = proofKey
            }, key);
            if (profileSisu != null)
            {
                try
                {
                    var auth = profileSisu.RootElement.GetProperty("AuthorizationToken");
                    var xui = auth.GetProperty("DisplayClaims").GetProperty("xui")[0];
                    xblXuid = xui.TryGetProperty("xid", out var xid) ? xid.GetString() : null;
                    xblGamertag = xui.TryGetProperty("gtg", out var gtg) ? gtg.GetString() : null;
                    xblAgeGroup = xui.TryGetProperty("agg", out var agg) ? agg.GetString() : null;
                    xblUhs = xui.TryGetProperty("uhs", out var uhs) ? uhs.GetString() : null;
                }
                catch { }
            }
        }

        // Export BCRYPT_ECCPRIVATE_BLOB
        var privParams = key.ExportParameters(true);
        var eccBlob = DeviceIdentity.ExportBcryptBlob(key);

        var payload = new Dictionary<string, object?>
        {
            ["_account_epoch"] = expectedAccountEpoch,
            ["device_id"] = deviceId,
            ["ecc_private_blob_b64"] = Convert.ToBase64String(eccBlob),
            ["device_token"] = deviceToken,
            ["device_token_expiry"] = deviceTokenExpiry,
            ["user_token"] = userToken,
            ["user_token_expiry"] = userTokenExpiry,
            ["xbl_token"] = xblToken,
            ["xbl_token_expiry"] = xblExpiry,
            ["xbl_xuid"] = xblXuid,
            ["xbl_gamertag"] = xblGamertag,
            ["xbl_age_group"] = xblAgeGroup,
            ["xbl_uhs"] = xblUhs,
            ["sisu_token"] = sisuToken, ["sisu_rp"] = sisuRp, ["sisu_uhs"] = sisuUhs, ["sisu_expiry"] = sisuExpiry,
            ["mp_token"] = mpToken, ["mp_rp"] = mpRp, ["mp_uhs"] = mpUhs, ["mp_expiry"] = mpExpiry,
            ["realms_token"] = realmsToken, ["realms_rp"] = realmsRp, ["realms_uhs"] = realmsUhs, ["realms_expiry"] = realmsExpiry,
            ["lic_token"] = licToken, ["lic_rp"] = licRp, ["lic_uhs"] = licUhs, ["lic_expiry"] = licExpiry,
            ["achievements_token"] = achievementsToken,
            ["achievements_uhs"] = achievementsUhs,
            ["achievements_expiry"] = achievementsExpiry,
            ["obtained"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(PathsList.DeviceJsonPath, json);
        try { File.SetUnixFileMode(PathsList.DeviceJsonPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }
        Console.WriteLine("Xbox Live pre-auth complete");
        return true;
    }
}