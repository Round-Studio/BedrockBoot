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

using System.ComponentModel;

namespace BedrockBoot.Win32;

partial class LaunchWindow
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LaunchWindow));
        label1 = new System.Windows.Forms.Label();
        GameNameBox = new System.Windows.Forms.Label();
        LaunchProgressBar = new System.Windows.Forms.ProgressBar();
        ProgressBox = new System.Windows.Forms.Label();
        SuspendLayout();
        // 
        // label1
        // 
        label1.Location = new System.Drawing.Point(12, 9);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(100, 23);
        label1.TabIndex = 0;
        label1.Visible = false;
        // 
        // GameNameBox
        // 
        GameNameBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)134));
        GameNameBox.Location = new System.Drawing.Point(12, 9);
        GameNameBox.Name = "GameNameBox";
        GameNameBox.Size = new System.Drawing.Size(340, 28);
        GameNameBox.TabIndex = 0;
        GameNameBox.Text = "启动游戏";
        // 
        // LaunchProgressBar
        // 
        LaunchProgressBar.Location = new System.Drawing.Point(12, 46);
        LaunchProgressBar.Name = "LaunchProgressBar";
        LaunchProgressBar.Size = new System.Drawing.Size(340, 20);
        LaunchProgressBar.TabIndex = 1;
        // 
        // ProgressBox
        // 
        ProgressBox.Location = new System.Drawing.Point(12, 69);
        ProgressBox.Name = "ProgressBox";
        ProgressBox.Size = new System.Drawing.Size(340, 23);
        ProgressBox.TabIndex = 2;
        ProgressBox.Text = "即将启动";
        ProgressBox.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
        // 
        // LaunchWindow
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.SystemColors.Control;
        ClientSize = new System.Drawing.Size(364, 101);
        ControlBox = false;
        Controls.Add(ProgressBox);
        Controls.Add(LaunchProgressBar);
        Controls.Add(GameNameBox);
        Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        Location = new System.Drawing.Point(15, 15);
        MaximizeBox = false;
        MaximumSize = new System.Drawing.Size(380, 140);
        MdiChildrenMinimizedAnchorBottom = false;
        MinimizeBox = false;
        MinimumSize = new System.Drawing.Size(380, 140);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "BedrockBoot - 快捷启动";
        ResumeLayout(false);
    }

    private System.Windows.Forms.ProgressBar LaunchProgressBar;
    private System.Windows.Forms.Label ProgressBox;

    private System.Windows.Forms.Label GameNameBox;

    private System.Windows.Forms.Label label1;

    #endregion
}