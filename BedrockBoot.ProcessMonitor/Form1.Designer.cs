namespace BedrockBoot.ProcessMonitor;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        button1 = new Button();
        button2 = new Button();
        button3 = new Button();
        label1 = new Label();
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(347, 326);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "确定";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // button2
        // 
        button2.Location = new Point(12, 326);
        button2.Name = "button2";
        button2.Size = new Size(88, 23);
        button2.TabIndex = 1;
        button2.Text = "打开 Github";
        button2.UseVisualStyleBackColor = true;
        // 
        // button3
        // 
        button3.Location = new Point(106, 326);
        button3.Name = "button3";
        button3.Size = new Size(126, 23);
        button3.TabIndex = 2;
        button3.Text = "打开 Dump 文件夹";
        button3.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        label1.Font = new Font("Microsoft YaHei UI", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 134);
        label1.Location = new Point(12, 12);
        label1.Name = "label1";
        label1.Size = new Size(367, 41);
        label1.TabIndex = 3;
        label1.Text = "抱歉，我们发生了点错误...";
        // 
        // label2
        // 
        label2.Location = new Point(16, 63);
        label2.Name = "label2";
        label2.Size = new Size(363, 21);
        label2.TabIndex = 4;
        label2.Text = "进程 PID：";
        // 
        // label3
        // 
        label3.Location = new Point(16, 84);
        label3.Name = "label3";
        label3.Size = new Size(363, 21);
        label3.TabIndex = 5;
        label3.Text = "退出码：";
        // 
        // label4
        // 
        label4.Location = new Point(16, 105);
        label4.Name = "label4";
        label4.Size = new Size(363, 21);
        label4.TabIndex = 6;
        label4.Text = "崩溃时间戳：";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(434, 361);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(button3);
        Controls.Add(button2);
        Controls.Add(button1);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MaximumSize = new Size(450, 400);
        MinimizeBox = false;
        MinimumSize = new Size(450, 400);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "BedrockBoot 崩溃报告";
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;

    private System.Windows.Forms.Label label1;

    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.Button button3;

    #endregion
}