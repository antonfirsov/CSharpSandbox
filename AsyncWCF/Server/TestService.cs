using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TestService : ITestService
    {
        private CancellationTokenSource _cancellationTokenSource = null;
        private Task<string> _currentTask = null;
        private readonly object _monitor = new object();

        public string Ping()
        {
            return "Pong!";
        }

        public async Task<string> DoLongRunningOperation(string input)
        {
            Console.WriteLine("input: {0} | Service instance: {1}", input, GetHashCode());
            if (_currentTask != null)
            {
                throw new FaultException<string>("*** An previous DoLongRunningOperation() execution is already being in progress! ***");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _currentTask = LongRunningOperationImpl(input, _cancellationTokenSource.Token);

            string result;
            try
            {
                result = await _currentTask;
            }
            catch (TaskCanceledException)
            {
                result = "*** ABORTED RESULT ***";
            }
            
            _currentTask = null;
            _cancellationTokenSource = null;
            return result;
        }

        public void AbortLongRunningOperation()
        {
            if (_currentTask == null || _cancellationTokenSource == null)
            {
                throw new FaultException<string>("There is no execution to abort!");
            }
            
            _cancellationTokenSource.Cancel();
        }

        private static async Task<string> LongRunningOperationImpl(string input, CancellationToken cancellationToken)
        {
            await Task.Delay(2000, cancellationToken);
            return string.Format(input + " lol!");
        }
    }
}
