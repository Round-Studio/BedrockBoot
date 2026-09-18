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

using System.Xml.Linq;

namespace BedrockBoot.Core.Models.Helper;

public class PackageIdentity
{
    public string Name { get; set; }
    public string Publisher { get; set; }
    public string Version { get; set; }
    public string ProcessorArchitecture { get; set; }

    public static PackageIdentity ParseFromXml(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        XNamespace ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

        var identity = doc.Root?.Element(ns + "Identity");

        if (identity == null)
            return null;

        return new PackageIdentity
        {
            Name = identity.Attribute("Name")?.Value,
            Publisher = identity.Attribute("Publisher")?.Value,
            Version = identity.Attribute("Version")?.Value,
            ProcessorArchitecture = identity.Attribute("ProcessorArchitecture")?.Value
        };
    }
}