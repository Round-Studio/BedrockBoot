using System.Diagnostics;

namespace BedrockBoot.ProcessMonitor;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.Run(new Form2());
    }

    public class EfficientProcessMonitor
    {
        private Process monitoredProcess;
        private string targetProcessName;

        public void MonitorSpecificProcess(string processName)
        {
            targetProcessName = processName;

            // 首先检查进程是否已经在运行
            CheckExistingProcess();

            // 设置定时器检查新进程启动
            var checkTimer = new System.Timers.Timer(1000);
            checkTimer.Elapsed += (s, e) => CheckExistingProcess();
            checkTimer.AutoReset = true;
            checkTimer.Start();

            Console.WriteLine($"开始监控进程: {processName}");
        }

        private void CheckExistingProcess()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(targetProcessName);

                if (processes.Length > 0 && monitoredProcess == null)
                {
                    monitoredProcess = processes[0];
                    SetupProcessMonitoring(monitoredProcess);
                    Console.WriteLine($"监控进程: {targetProcessName} (PID: {monitoredProcess.Id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查进程时发生错误: {ex.Message}");
            }
        }

        private void SetupProcessMonitoring(Process process)
        {
            try
            {
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    int exitCode = process.ExitCode;
                    Console.WriteLine($"进程退出: {targetProcessName}, 退出代码: {exitCode}");

                    if (exitCode != 0)
                    {
                        Console.WriteLine($"进程异常退出（崩溃）: {targetProcessName}");
                        // 处理崩溃逻辑
                        new Form1().ShowDialog();
                    }

                    monitoredProcess = null;
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置进程监控时发生错误: {ex.Message}");
            }
        }

        public void StopMonitoring()
        {
            monitoredProcess?.Dispose();
        }
    }
}