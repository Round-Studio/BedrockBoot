using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Avalonia.Controls;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Loader.PreLoad.NET;
using BedrockBoot.Views.Control.Items.Instance.PreLoad.NET;

namespace BedrockBoot.Views.DialogContent.Loader.PreLoad.NET;

public partial class DialogPreviewFunctionContent : UserControl
{
    private readonly VersionConfig _instance;
    private readonly List<FeatureItem> _items = new();

    private readonly string _featuresDocsPath;
    private readonly string _featuresConfigPath;

    public DialogPreviewFunctionContent()
    {
        InitializeComponent();
        _featuresDocsPath = _featuresConfigPath = "";
    }

    public DialogPreviewFunctionContent(VersionConfig instance) : this()
    {
        _instance = instance;
        _featuresDocsPath = Path.Combine(instance.VersionPath!, "config", "PreLoad.NET", "features.doc.xml");
        _featuresConfigPath = Path.Combine(instance.VersionPath!, "config", "PreLoad.NET", "features.xml");
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!File.Exists(_featuresDocsPath) || !File.Exists(_featuresConfigPath))
            return;
        var doc = LoadDoc(_featuresDocsPath);
        var cfg = LoadDoc(_featuresConfigPath);

        var cfgMap = new Dictionary<string, FeatureXml.FeatureDoc>(StringComparer.Ordinal);
        foreach (var c in cfg.Features)
        {
            if (string.IsNullOrEmpty(c.Name))
                continue;
            cfgMap[c.Name] = c;
        }

        InfoCard.IsVisible = doc.Features.Count == 0;

        foreach (var f in doc.Features)
        {
            if (string.IsNullOrEmpty(f.Name))
                continue;

            var enabled = cfgMap.TryGetValue(f.Name, out var c) ? c.Enabled : f.Default;

            var item = new FeatureItem(f, enabled);
            FunctionStackPanel.Children.Add(item);
            _items.Add(item);
        }
    }

    private static FeatureXml.FeaturesDoc LoadDoc(string path)
    {
        var serializer = new XmlSerializer(typeof(FeatureXml.FeaturesDoc));
        using var reader = new StringReader(File.ReadAllText(path));
        return (FeatureXml.FeaturesDoc)serializer.Deserialize(reader)!;
    }

    public void Save()
    {
        if (!File.Exists(_featuresConfigPath)) return;
        var cfg = LoadDoc(_featuresConfigPath);
        var cfgMap = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var c in cfg.Features)
        {
            if (string.IsNullOrEmpty(c.Name))
                continue;
            cfgMap[c.Name] = c.Enabled;
        }

        foreach (var item in _items)
        {
            var (name, enabled) = item.GetEnable();
            if (string.IsNullOrEmpty(name))
                continue;
            cfgMap[name] = enabled;
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
        };

        var tmp = _featuresConfigPath + ".tmp";
        using (var writer = XmlWriter.Create(tmp, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("features");

            writer.WriteComment(
                "Feature configuration for PreLoad.NET. This file is auto-generated on first startup, you can edit it to enable/disable features.");

            foreach (var kv in cfgMap)
            {
                writer.WriteStartElement("feature");
                writer.WriteAttributeString("name", kv.Key);
                writer.WriteAttributeString("enabled", kv.Value ? "true" : "false");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        File.Move(tmp, _featuresConfigPath, overwrite: true);
    }
}