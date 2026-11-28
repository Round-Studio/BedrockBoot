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
using System.Text;
#if WINDOWS
using System.Security.Cryptography;
#endif

namespace BedrockBoot.Models.Helper;

/// <summary>
///     账户凭证落盘时的保护。
///     <para>
///     Windows 用 DPAPI（<see cref="DataProtectionScope.CurrentUser" />）：密文绑定"这台机器 + 这个
///     Windows 账户"，换用户、换机器、文件被同步或打包带走都解不开；同一用户下运行的其它程序仍能解密，
///     这一层只解决"文件离开本机"。
///     </para>
///     <para>
///     非 Windows 平台没有 DPAPI，按原样保存（<see cref="IsAvailable" /> 为 false），调用方据此决定
///     是否要收紧文件权限；带前缀的密文在这些平台会被判为"凭证失效"，引导重新登录。
///     </para>
/// </summary>
public static class SecretProtector
{
    /// <summary>密文标记：带这个前缀的值才走解密，旧版本的明文值原样返回</summary>
    private const string Prefix = "dpapi:v1:";

    /// <summary>
    ///     附加熵。它不是密钥（就编在程序里），作用只是让别的程序没法把这段密文直接丢给
    ///     <c>CryptUnprotectData</c> 解开，属于门槛而不等于保险。
    /// </summary>
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BedrockBoot.Account.Secret.v1");

    /// <summary>
    ///     当前环境是否真的能加解密。编译到 Windows 只是前提，精简版系统 / 损坏的加密服务提供程序
    ///     会让 <c>CryptProtectData</c> 直接失败（本机实测返回 127），所以用一次往返探测确认，
    ///     结果缓存；探测失败时凭证按明文保存，功能不受影响。
    /// </summary>
    public static bool IsAvailable => Availability.Value;

    private static readonly Lazy<bool> Availability = new(Probe);

    private static bool Probe()
    {
#if WINDOWS
        try
        {
            var probe = Encoding.UTF8.GetBytes("BedrockBoot.SecretProtector.Probe");
            var cipher = ProtectedData.Protect(probe, Entropy, DataProtectionScope.CurrentUser);
            var plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
            return plain.AsSpan().SequenceEqual(probe);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[SecretProtector] 系统 DPAPI 不可用，凭证将以明文保存：{ex.Message}");
            return false;
        }
#else
        return false;
#endif
    }

    /// <summary>该值是否已经是密文</summary>
    public static bool IsProtected(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>明文 → 落盘值。空值、已是密文、或当前环境用不了 DPAPI 时原样返回。</summary>
    public static string? Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain) || IsProtected(plain) || !IsAvailable) return plain;

#if WINDOWS
        try
        {
            var cipher = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(cipher);
        }
        catch (Exception ex)
        {
            // 用户配置文件异常等极端情况：退回明文，至少不把登录弄坏
            Console.WriteLine($@"[SecretProtector] 加密失败，按明文保存：{ex.Message}");
            return plain;
        }
#else
        return plain;
#endif
    }

    /// <summary>
    ///     落盘值 → 明文。解不开（换了机器/重装系统/密文损坏）返回 <c>null</c>，
    ///     调用方应当把它当成"凭证失效"，走重新登录，而不是抛异常。
    /// </summary>
    public static string? Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored) || !IsProtected(stored)) return stored;

#if WINDOWS
        try
        {
            var cipher = Convert.FromBase64String(stored[Prefix.Length..]);
            var plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[SecretProtector] 解密失败，按凭证失效处理：{ex.Message}");
            return null;
        }
#else
        // 非 Windows 平台没有 DPAPI，来自 Windows 的配置在这里必然解不开
        return null;
#endif
    }
}
