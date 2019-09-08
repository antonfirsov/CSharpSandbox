using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace RunPs
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (PowerShell powerShell = PowerShell.Create())
            {
                powerShell.AddScript(@"Set-Location C:\Dell");
                powerShell.AddScript(Script);

                Collection<PSObject> psOutput = powerShell.Invoke();
                Console.WriteLine("----");

                string retStr = psOutput.FirstOrDefault()?.BaseObject as string;
                Console.WriteLine(retStr);
            }

            Console.WriteLine("kabbe");
            Console.ReadLine();
        }

        private const string Script = @"
return (Get-Location).Path

$wut = Start-Process -FilePath 'C:\Windows\System32\net.exe' -ArgumentList 'accounts' -WorkingDirectory 'C:\Windows\System32\' -Wait -NoNewWindow -PassThru

if ($wut.ExitCode -eq 0) {
    return 'jee';
}
else {
    return 'boo'
}
";
    }
}