namespace BedrockBoot.ProcessMonitor;

public partial class Form2 : Form
{
    public Form2()
    {
        InitializeComponent();

        Visible = false;
        Hide();
        
        var monitor = new Program.EfficientProcessMonitor();
        monitor.MonitorSpecificProcess("BedrockBoot");
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_APPWINDOW = 0x40000;
            const int WS_EX_TOOLWINDOW = 0x80;
            CreateParams cp = base.CreateParams;
            cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在任务栏:cite[8]
            cp.ExStyle |= WS_EX_TOOLWINDOW;       // 不显示在Alt+Tab:cite[8]
            return cp;
        }
    }
}