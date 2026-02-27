using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Win32.Controls;

namespace BedrockBoot.Win32;

public partial class ImportResourcePack : Form
{
    private readonly string _file;
    public List<VersionConfig> Games;

    public ImportResourcePack()
    {
        InitializeComponent();
    }

    public ImportResourcePack(List<string> args) : this()
    {
        _file = args[args.FindIndex(a => a == "-open") + 1];
        var y = 0;
        new ResourcePackAnalysis(_file).GetPackManifests().ForEach(conf =>
        {
            // Add(new PackItem(conf));
            panel1.Controls.Add(new PackItem(conf)
            {
                Height = 80,
                Location = new Point(0, y)
            });
            y += 80;
        });

        if (GlobalModel.Config.Data.GameFolders.Count <= 0)
        {
            Close();
            MessageBox.Show("当前配置环境中无可用游戏目录，\n请前往 本体>实例管理 中添加游戏目录", @"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        GlobalModel.Config.Data.GameFolders.ForEach(f =>
            comboBox2.Items.Add($"{f.GameFolderName} - {f.GameFolderPath}"));
        comboBox2.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
    }

    private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
    {
        Games = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data.GameFolders[comboBox2.SelectedIndex]
            .GameFolderPath);
        comboBox1.Items.Clear();
        button1.Enabled = true;
        Games.ForEach(g => { comboBox1.Items.Add($"{g.Info.VersionName} - {g.Info.Version}"); });

        if (Games.Count > 0)
            comboBox1.SelectedIndex = 0;
        else
            button1.Enabled = false;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        var man = new ResourcePackManager(GameInfoHelper.GetVersionConfig(Games[comboBox1.SelectedIndex].VersionPath));
        man.GetAllPack();
        man.AddRangePacks(new List<string> { _file });

        if (MessageBox.Show("导入包成功", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            Close();
    }
}