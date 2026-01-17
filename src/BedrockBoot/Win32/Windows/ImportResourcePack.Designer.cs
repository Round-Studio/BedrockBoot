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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportResourcePack));
        button1 = new System.Windows.Forms.Button();
        comboBox1 = new System.Windows.Forms.ComboBox();
        comboBox2 = new System.Windows.Forms.ComboBox();
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        panel1 = new System.Windows.Forms.Panel();
        SuspendLayout();
        // 
        // button1
        // 
        button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
        button1.Location = new System.Drawing.Point(552, 503);
        button1.Name = "button1";
        button1.Size = new System.Drawing.Size(100, 36);
        button1.TabIndex = 0;
        button1.Text = "开始导入";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // comboBox1
        // 
        comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        comboBox1.FormattingEnabled = true;
        comboBox1.Location = new System.Drawing.Point(74, 472);
        comboBox1.Name = "comboBox1";
        comboBox1.Size = new System.Drawing.Size(578, 25);
        comboBox1.TabIndex = 1;
        // 
        // comboBox2
        // 
        comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        comboBox2.FormattingEnabled = true;
        comboBox2.Location = new System.Drawing.Point(74, 441);
        comboBox2.Name = "comboBox2";
        comboBox2.Size = new System.Drawing.Size(578, 25);
        comboBox2.TabIndex = 2;
        comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new System.Drawing.Point(12, 475);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(56, 17);
        label1.TabIndex = 3;
        label1.Text = "目标实例";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new System.Drawing.Point(12, 444);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(56, 17);
        label2.TabIndex = 4;
        label2.Text = "游戏目录";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new System.Drawing.Point(12, 9);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(116, 17);
        label3.TabIndex = 5;
        label3.Text = "您将会导入以下包：";
        // 
        // panel1
        // 
        panel1.Location = new System.Drawing.Point(12, 29);
        panel1.Name = "panel1";
        panel1.Size = new System.Drawing.Size(640, 406);
        panel1.TabIndex = 6;
        // 
        // ImportResourcePack
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(664, 551);
        Controls.Add(panel1);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(comboBox2);
        Controls.Add(comboBox1);
        Controls.Add(button1);
        Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        MaximizeBox = false;
        MaximumSize = new System.Drawing.Size(680, 590);
        MinimizeBox = false;
        MinimumSize = new System.Drawing.Size(680, 590);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "BedrockBoot - 导入文件";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Button button1;
    private ComboBox comboBox1;
    private System.Windows.Forms.ComboBox comboBox2;
    private Label label1;
    private Label label2;
    private Label label3;
    private Panel panel1;
}