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

using System.Reflection;

namespace BedrockBoot.Localization;

/// <summary>
///     本仓库内置文案的位置：全部语言文件都打包在本程序集的 <c>Resources/I18n</c> 下。
/// </summary>
/// <remarks>
///     把文案集中在本程序集（而不是每个模块各带一份）是为了让翻译只有一份来源：
///     模块之间用键前缀区分归属（<c>Core.*</c> / <c>Windows.*</c> / <c>Linux.*</c>），
///     而不是各自维护一套语言文件。
/// </remarks>
public static class BuiltinStrings
{
    /// <summary>语言文件在程序集内的目录，用于 <see cref="ResourceStringModule.LoadFrom" />。</summary>
    public const string Folder = "Resources/I18n";

    /// <summary>语言目录的 <c>avares://</c> 形式，用于构造 <see cref="ResourceStringModule" />。</summary>
    public const string BaseUri = "avares://BedrockBoot.Localization/" + Folder;

    /// <summary>内置来源的标识，用于判重。</summary>
    public const string ModuleId = "BedrockBoot";

    /// <summary>内置文案的程序集。</summary>
    public static Assembly Assembly => typeof(BuiltinStrings).Assembly;

    /// <summary>创建指向内置文案的来源。</summary>
    public static ResourceStringModule CreateModule() => new(ModuleId, BaseUri);

    /// <summary>
    ///     确保内置来源已在 <paramref name="context" /> 上注册（幂等）。
    /// </summary>
    /// <remarks>
    ///     去掉"忘了调注册方法就整表空白、所有文案显示键名"这个坑：
    ///     <see cref="LocalizationContext" /> 在构造时就会调用本方法，
    ///     因此任何宿主（测试、命令行工具、插件宿主）零初始化即可查串。
    /// </remarks>
    public static void RegisterInto(LocalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Catalog.Contains(ModuleId))
            return;

        context.Register(CreateModule(), apply: false);
    }

    /// <summary>直接读取一门语言，供 UI 框架尚未初始化时（如启动早期）使用。</summary>
    public static Dictionary<string, string>? Load(string culture) =>
        ResourceStringModule.LoadFrom(Assembly, Folder, culture);
}
