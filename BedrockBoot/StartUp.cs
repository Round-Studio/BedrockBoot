using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinRT;

namespace BedrockBoot
{
    public static class StartUp
    {
        [DebuggerNonUserCode]
        [STAThread]
        public static void Main(string[] args)
        {
            if (!System.IO.Directory.Exists("crashdump"))
            {
                System.IO.Directory.CreateDirectory("crashdump");
            }
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                string dumpFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, string.Format("crashdump\\crash-dump-{0}.dmp", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff")));
                MiniDump.Write(dumpFile);
            };
          
                  ComWrappersSupport.InitializeComWrappers(null);
	        Application.Start(delegate(ApplicationInitializationCallbackParams p)
        	{
	        	DispatcherQueueSynchronizationContext context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
	           	SynchronizationContext.SetSynchronizationContext(context);
		        new App();
            });
            Application.Current.UnhandledException += (o, e) =>
            {
                string dumpFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, string.Format("crashdump\\crash-dump-{0}.dmp", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff")));
                MiniDump.Write(dumpFile);
            };
        }
    }
}
