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