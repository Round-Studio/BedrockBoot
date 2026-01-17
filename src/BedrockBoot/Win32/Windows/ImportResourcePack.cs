using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Win32.Controls;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BedrockBoot.Win32;

public partial class ImportResourcePack : Form
{
    public ImportResourcePack()
    {
        InitializeComponent();
    }

    public ImportResourcePack(List<string> args) : this()
    {
        var file = args[args.FindIndex(a => a == "-open") + 1];
        new ResourcePackAnalysis(file).GetPackManifests().ForEach(conf =>
        {
            listBox1.Items.Add(new PackItem(conf));
        });
    }
}