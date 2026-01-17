using System.ComponentModel;
using System.Windows.Forms;

namespace BedrockBoot.Win32.Controls;

partial class PackItem
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

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        PackIcon = new PictureBox();
        PackName = new Label();
        ((ISupportInitialize)PackIcon).BeginInit();
        SuspendLayout();
        // 
        // PackIcon
        // 
        PackIcon.Location = new System.Drawing.Point(3, 3);
        PackIcon.Name = "PackIcon";
        PackIcon.Size = new System.Drawing.Size(64, 64);
        PackIcon.TabIndex = 0;
        PackIcon.TabStop = false;
        // 
        // PackName
        // 
        PackName.AutoSize = true;
        PackName.Location = new System.Drawing.Point(73, 3);
        PackName.Name = "PackName";
        PackName.Size = new System.Drawing.Size(43, 17);
        PackName.TabIndex = 1;
        PackName.Text = "label1";
        // 
        // PackItem
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(PackName);
        Controls.Add(PackIcon);
        Name = "PackItem";
        Size = new System.Drawing.Size(612, 70);
        ((ISupportInitialize)PackIcon).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private PictureBox PackIcon;
    private Label PackName;
}