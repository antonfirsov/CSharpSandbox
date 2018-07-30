using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UserProcess;

namespace TestService
{
    public partial class TheTestService : ServiceBase
    {
        private ILogger _logger = new Logger();

        public TheTestService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread(RunStuff);
            thread.Start();
        }

        protected override void OnStop()
        {
        }

        private void RunStuff()
        {
            _logger.Log("Hello service!");

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logger.Log("localAppData from svc: " + localAppData);

            Thread.Sleep(15000);
            _logger.Log("Yay after 15 sec!");
            
            
            string exePath =
                @"c:\dev\sandbox\csharp\DotNetSandbox\WindowsServiceSandbox\TestConsole\bin\Debug\TestConsole.exe";
            _logger.Log("Running "+exePath);

            ProcessStarter ps = new ProcessStarter(exePath, _logger);
            ps.Run();
            //ProcessAsCurrentUser stuff = new ProcessAsCurrentUser(_logger);
            //stuff.CreateProcessAsCurrentUser(exePath);
            _logger.Log("Done.");
        }
    }
}
