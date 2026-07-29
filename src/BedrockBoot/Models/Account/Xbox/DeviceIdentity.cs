using System;
using System.IO;
using System.Security.Cryptography;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Account.Xbox;

public static class DeviceIdentity
{
    static readonly byte[] Pkcs8Header = [
        0x30, 0x77, 0x02, 0x01, 0x01, 0x04, 0x20,
        0xA0, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07,
        0xA1, 0x44, 0x03, 0x42, 0x00
    ];

    public static ECDsa LoadOrCreateKey()
    {
        if (File.Exists(PathsList.DeviceKeyPath))
        {
            try
            {
                var pem = File.ReadAllText(PathsList.DeviceKeyPath);
                var loaded = LoadKeyFromPem(pem);
                Console.WriteLine($"Loaded existing device key from {PathsList.DeviceKeyPath}");
                return loaded;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load device key: {e.Message}, creating new one");
            }
        }
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SaveKey(key);
        Console.WriteLine("Created new device key");
        return key;
    }

    static ECDsa LoadKeyFromPem(string pem)
    {
        var b64 = pem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "").Replace("\r", "");
        var der = Convert.FromBase64String(b64);
        // Parse PKCS8: version[1], algorithmIdentifier[13], privateKey[32], [publicKey[44]]
        var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(der, out _);
        return key;
    }

    static void SaveKey(ECDsa key)
    {
        Directory.CreateDirectory(PathsList.PreauthDir);
        var der = key.ExportPkcs8PrivateKey();
        var b64 = Convert.ToBase64String(der);
        var pem = "-----BEGIN PRIVATE KEY-----\n";
        for (int i = 0; i < b64.Length; i += 64)
            pem += b64.Substring(i, Math.Min(64, b64.Length - i)) + "\n";
        pem += "-----END PRIVATE KEY-----\n";
        File.WriteAllText(PathsList.DeviceKeyPath, pem);
        try { File.SetUnixFileMode(PathsList.DeviceKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }
    }

    public static string LoadOrCreateDeviceId()
    {
        if (File.Exists(PathsList.DeviceIdPath))
            return File.ReadAllText(PathsList.DeviceIdPath).Trim();
        var id = "{" + Guid.NewGuid().ToString().ToUpper() + "}";
        File.WriteAllText(PathsList.DeviceIdPath, id);
        return id;
    }

    public static byte[] ExportBcryptBlob(ECDsa key)
    {
        var priv = key.ExportParameters(true);
        var blob = new byte[104];
        BitConverter.GetBytes(0x32534345).CopyTo(blob, 0);
        BitConverter.GetBytes(32).CopyTo(blob, 4);
        priv.Q.X!.CopyTo(blob, 8);
        priv.Q.Y!.CopyTo(blob, 40);
        priv.D!.CopyTo(blob, 72);
        return blob;
    }
}