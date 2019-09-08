using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace InvokeProcessWithReturnCode
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await InvokeExeDirectly(true);
            await InvokeExeDirectly(false);
            await InvokeUsingCmd(true);
            await InvokeUsingCmd(false);
            await InvokeBatchFile(true);
            await InvokeBatchFile(false);
            
            //Console.ReadLine();
        }

        private static async Task InvokeExeDirectly(bool success)
        {
            Console.WriteLine($"{nameof(InvokeExeDirectly)}({success})");
            
            string fileName = "ExeWithReturnCode.exe";
            string arguments =  success ? "" : "make it fail";
            
            int exitCode = await InvokeProcess(fileName, arguments);
            Console.WriteLine("EXIT CODE: " + exitCode);
        }

        private static async Task InvokeUsingCmd(bool success)
        {
            Console.WriteLine($"{nameof(InvokeUsingCmd)}({success})");
            
            string fileName = "cmd.exe";
            string failText = success ? "" : " make it fail";
            string arguments = "ExeWithReturnCode.exe" + failText;
         
            int exitCode = await InvokeProcess(fileName, arguments);
            Console.WriteLine("EXIT CODE: " + exitCode);
        }

        private static async Task InvokeBatchFile(bool success)
        {
            Console.WriteLine($"{nameof(InvokeBatchFile)}({success})");
            
            string fileName = "cmd.exe";
            string arguments = success ? "Invoke-Success.bat" : "Invoke-Fail.bat";
            int exitCode = await InvokeProcess(fileName, arguments);
            Console.WriteLine("EXIT CODE: " + exitCode);
        }

        private static async Task<int> InvokeProcess(string fileName, string arguments)
        {
            Console.WriteLine("***********************");
            Console.WriteLine($"{fileName} {arguments}");
            
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

                try
                {
                    process.Start();
                    process.WaitForExit();
                    await Task.CompletedTask;
                    //await WaitForExitAsync(process);
                    return process.ExitCode;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 666;
                }
            }
        }

        /// <summary>
        /// A better, async variant of Process.WaitForExit()
        /// </summary>
        private static async Task WaitForExitAsync(Process process)
        {
            var tcs = new TaskCompletionSource<bool>();

            void ProcessExited(object sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;

            try
            {
                if (process.HasExited)
                {
                    return;
                }
                
                await tcs.Task;
            }
            finally
            {
                process.Exited -= ProcessExited;
            }
        }
    }
}