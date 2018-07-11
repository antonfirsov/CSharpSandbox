using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Client.ServiceReferences;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {   
            Task work = DoWork();
            work.ConfigureAwait(false);
            work.Wait();

            Console.ReadLine();
        }

        private static async Task DoWork()
        {
            BasicSynchronousCall();

            await LongRunningOperation_RunUntilFinshed();
            await LongRunningOperation_WhenStartedTwice_ShouldFail();
            await LongRunningOperation_Cancel();
        }

        private static void BasicSynchronousCall()
        {
            Console.WriteLine("\n\n=== Example 0: LongRunningOperation_RunUntilFinshed ===");
            DoWcfCall(c => { Console.WriteLine(c.Ping()); });
        }

        private static Task LongRunningOperation_RunUntilFinshed()
        {
            Console.WriteLine("\n\n=== Example 1: LongRunningOperation_RunUntilFinshed ===");
            return DoWcfCallAsync(async c =>
            {
                Console.WriteLine("Starting DoLongRunningOperationAsync ...");
                string result = await c.DoLongRunningOperationAsync("goat");
                Console.WriteLine("... Done! RESULT: " + result);
            });    
        }

        private static async Task LongRunningOperation_WhenStartedTwice_ShouldFail()
        {
            Console.WriteLine("\n\n=== Example 2: LongRunningOperation_WhenStartedTwice_ShouldFail ===");

            Task firstExecution = DoWcfCallAsync(c =>
            {
                Console.WriteLine("Starting DoLongRunningOperationAsync first time ...");
                return c.DoLongRunningOperationAsync("snail");
            });

            
            // wait a little bit, just because:
            await Task.Delay(50);

            bool thrown = false;
            try
            {
                await DoWcfCallAsync(c =>
                {
                    Console.WriteLine("Starting DoLongRunningOperationAsync second time.");
                    Console.WriteLine("It's implementation should throw!");
                    return c.DoLongRunningOperationAsync("bear");
                });
            }
            catch (FaultException<string> ex)
            {
                Console.WriteLine("FaultException<string> has been thrown as expected!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Detail);
                thrown = true;
            }

            if (!thrown)
            {
                Console.WriteLine("No exception has been thrown :(((");
            }

            await firstExecution;
        }

        private static async Task LongRunningOperation_Cancel()
        {
            Console.WriteLine("\n\n=== Example 3: LongRunningOperation_Cancel ===");

            await DoWcfCallAsync(async c =>
            {
                Console.WriteLine("Starting DoLongRunningOperationAsync('camel') just to abort it immediately ...");
                Task<string> executionTask = c.DoLongRunningOperationAsync("camel");
                // wait a little bit:
                await Task.Delay(50);
                Console.WriteLine("Aborting ...");
                c.AbortLongRunningOperation();
                Console.WriteLine("Aborted!");

                Console.WriteLine("Awaiting the original task ...");
                string result = await executionTask;
                Console.WriteLine("Done! RESULT: "+result);
            });
        }


        private static void DoWcfCall(Action<TestServiceClient> action)
        {
            using (TestServiceClient client = new TestServiceClient("NetTcpBinding_ITestService"))
            {
                action(client);
            }
        }

        private static async Task DoWcfCallAsync(Func<TestServiceClient, Task> action)
        {
            using (TestServiceClient client = new TestServiceClient("NetTcpBinding_ITestService"))
            {
                await action(client);
            }
        }
    }
}
