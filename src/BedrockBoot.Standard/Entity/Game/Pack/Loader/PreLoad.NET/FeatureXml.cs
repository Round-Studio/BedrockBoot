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

using System.Xml.Serialization;

namespace BedrockBoot.Standard.Entity.Game.Pack.Loader.PreLoad.NET;

public class FeatureXml
{
    [XmlRoot("features")]
    public class FeaturesDoc
    {
        [XmlElement("feature")]
        public List<FeatureDoc> Features { get; set; } = new();
    }

    public class FeatureDoc
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("default")]
        public bool Default { get; set; }

        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; } = "";

        [XmlElement("description")]
        public string Description { get; set; } = "";
    }
}