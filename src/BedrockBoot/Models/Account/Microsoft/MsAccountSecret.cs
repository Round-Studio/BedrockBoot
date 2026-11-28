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

using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Account.Microsoft;

namespace BedrockBoot.Models.Account.Microsoft;

/// <summary>
///     MsAccounts.json 里凭证字段的加解密。只处理 <c>AuthResult</c> 的 access_token / refresh_token，
///     用户名、头像、BUID、过期时间等保持明文，方便排查。
///     <para>
///     这套逻辑不能放进 <see cref="MsUserConfig" /> 实体：实体所在的 BedrockBoot.Standard 没有引用
///     WindowsDesktop 框架，拿不到 DPAPI。所以改由管理器在读写边界上处理。
///     </para>
/// </summary>
internal static class MsAccountSecret
{
    /// <summary>是否还有明文凭证需要迁移成密文</summary>
    public static bool HasPlaintextCredential(MsUserConfigRoot? root)
    {
        if (!SecretProtector.IsAvailable || root?.Accounts is null) return false;

        foreach (var account in root.Accounts)
        {
            var auth = account.AuthResult;
            if (auth is null) continue;

            if (!string.IsNullOrEmpty(auth.AccessToken) && !SecretProtector.IsProtected(auth.AccessToken))
                return true;
            if (!string.IsNullOrEmpty(auth.RefreshToken) && !SecretProtector.IsProtected(auth.RefreshToken))
                return true;
        }

        return false;
    }

    /// <summary>明文 → 密文（写盘前调用）</summary>
    public static void ProtectInPlace(MsUserConfigRoot? root)
    {
        if (root?.Accounts is null) return;

        foreach (var account in root.Accounts)
        {
            var auth = account.AuthResult;
            if (auth is null) continue;

            auth.AccessToken = SecretProtector.Protect(auth.AccessToken);
            auth.RefreshToken = SecretProtector.Protect(auth.RefreshToken);
        }
    }

    /// <summary>
    ///     密文 → 明文（读盘后、写盘结束都要调用）。解不开时置空，
    ///     该账户会在刷新时被判为缺少 refresh_token 而跳过，不会抛异常。
    /// </summary>
    public static void UnprotectInPlace(MsUserConfigRoot? root)
    {
        if (root?.Accounts is null) return;

        foreach (var account in root.Accounts)
        {
            var auth = account.AuthResult;
            if (auth is null) continue;

            auth.AccessToken = SecretProtector.Unprotect(auth.AccessToken);
            auth.RefreshToken = SecretProtector.Unprotect(auth.RefreshToken);
        }
    }
}
