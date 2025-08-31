namespace BedrockBoot.ProcessMonitor;

public partial class Form2 : Form
{
    public Form2()
    {
        InitializeComponent();
        
        var monitor = new Program.EfficientProcessMonitor();
        monitor.MonitorSpecificProcess("BedrockBoot");
    }
}