using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BedrockBoot.Win32;

partial class ImportResourcePack
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(ImportResourcePack));
        button1 = new Button();
        comboBox1 = new ComboBox();
        comboBox2 = new ComboBox();
        label1 = new Label();
        label2 = new Label();
        label3 = new Label();
        listBox1 = new ListBox();
        SuspendLayout();
        // 
        // button1
        // 
        button1.FlatStyle = FlatStyle.System;
        button1.Location = new Point(552, 503);
        button1.Name = "button1";
        button1.Size = new Size(100, 36);
        button1.TabIndex = 0;
        button1.Text = "开始导入";
        button1.UseVisualStyleBackColor = true;
        // 
        // comboBox1
        // 
        comboBox1.FormattingEnabled = true;
        comboBox1.Location = new Point(74, 472);
        comboBox1.Name = "comboBox1";
        comboBox1.Size = new Size(578, 25);
        comboBox1.TabIndex = 1;
        // 
        // comboBox2
        // 
        comboBox2.FormattingEnabled = true;
        comboBox2.Location = new Point(74, 441);
        comboBox2.Name = "comboBox2";
        comboBox2.Size = new Size(578, 25);
        comboBox2.TabIndex = 2;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(12, 475);
        label1.Name = "label1";
        label1.Size = new Size(56, 17);
        label1.TabIndex = 3;
        label1.Text = "目标实例";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(12, 444);
        label2.Name = "label2";
        label2.Size = new Size(56, 17);
        label2.TabIndex = 4;
        label2.Text = "游戏目录";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(12, 9);
        label3.Name = "label3";
        label3.Size = new Size(116, 17);
        label3.TabIndex = 5;
        label3.Text = "您将会导入以下包：";
        // 
        // listBox1
        // 
        listBox1.FormattingEnabled = true;
        listBox1.Location = new Point(12, 29);
        listBox1.Name = "listBox1";
        listBox1.Size = new Size(640, 395);
        listBox1.TabIndex = 6;
        // 
        // ImportResourcePack
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(664, 551);
        Controls.Add(listBox1);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(comboBox2);
        Controls.Add(comboBox1);
        Controls.Add(button1);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MaximumSize = new Size(680, 590);
        MinimizeBox = false;
        MinimumSize = new Size(680, 590);
        Name = "ImportResourcePack";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "BedrockBoot - 导入文件";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button button1;
    private ComboBox comboBox1;
    private ComboBox comboBox2;
    private Label label1;
    private Label label2;
    private Label label3;
    private ListBox listBox1;
}