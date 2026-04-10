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