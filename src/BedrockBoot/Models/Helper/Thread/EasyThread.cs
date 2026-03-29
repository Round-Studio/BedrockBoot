using System;

namespace BedrockBoot.Models.Helper.Thread;

public class EasyThread
{
    public static void Run(Action action)
    {
        new System.Threading.Thread(() => action.Invoke()).Start();
    }
}