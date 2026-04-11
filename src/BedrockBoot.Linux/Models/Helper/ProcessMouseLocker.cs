using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using BedrockBoot.Models.Helper.Notice;

namespace BedrockBoot.Models.Helper;

public class ProcessMouseLocker
{
    public ProcessMouseLocker(int processId)
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }

    /// <summary>
    /// 开启鼠标锁定监控逻辑
    /// </summary>
    public void Start()
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }

    /// <summary>
    /// 停止监控并释放鼠标
    /// </summary>
    public void Stop()
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }
}