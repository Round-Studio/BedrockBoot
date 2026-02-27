using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BedrockBoot.Win32;

partial class DownloadVCWindow
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
        DownloadProgress = new System.Windows.Forms.ProgressBar();
        label1 = new System.Windows.Forms.Label();
        linkLabel1 = new System.Windows.Forms.LinkLabel();
        linkLabel2 = new System.Windows.Forms.LinkLabel();
        SuspendLayout();
        // 
        // DownloadProgress
        // 
        DownloadProgress.Location = new System.Drawing.Point(12, 35);
        DownloadProgress.Name = "DownloadProgress";
        DownloadProgress.Size = new System.Drawing.Size(360, 23);
        DownloadProgress.TabIndex = 0;
        // 
        // label1
        // 
        label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
        label1.Location = new System.Drawing.Point(12, 9);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(349, 23);
        label1.TabIndex = 1;
        label1.Text = "BedrockBoot Visual C++ 运行库下载程序";
        // 
        // linkLabel1
        // 
        linkLabel1.Location = new System.Drawing.Point(12, 69);
        linkLabel1.Name = "linkLabel1";
        linkLabel1.Size = new System.Drawing.Size(152, 23);
        linkLabel1.TabIndex = 2;
        linkLabel1.TabStop = true;
        linkLabel1.Text = "BedrockBoot | 常见问题";
        linkLabel1.LinkClicked += linkLabel1_LinkClicked;
        // 
        // linkLabel2
        // 
        linkLabel2.Location = new System.Drawing.Point(170, 69);
        linkLabel2.Name = "linkLabel2";
        linkLabel2.Size = new System.Drawing.Size(62, 23);
        linkLabel2.TabIndex = 3;
        linkLabel2.TabStop = true;
        linkLabel2.Text = "产品文档";
        linkLabel2.LinkClicked += linkLabel2_LinkClicked;
        // 
        // DownloadVCWindow
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(384, 101);
        ControlBox = false;
        Controls.Add(linkLabel2);
        Controls.Add(linkLabel1);
        Controls.Add(label1);
        Controls.Add(DownloadProgress);
        MaximumSize = new System.Drawing.Size(400, 140);
        MinimumSize = new System.Drawing.Size(400, 140);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "VC 2015-2022 Downloader";
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.LinkLabel linkLabel1;
    private System.Windows.Forms.LinkLabel linkLabel2;

    private System.Windows.Forms.ProgressBar DownloadProgress;

    #endregion
}