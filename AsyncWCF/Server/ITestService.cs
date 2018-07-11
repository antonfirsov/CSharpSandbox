using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITestService" in both code and config file together.
    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        string Ping();

        [OperationContract]
        [FaultContract(typeof(string))]
        Task<string> DoLongRunningOperation(string input);

        [OperationContract]
        [FaultContract(typeof(string))]
        void AbortLongRunningOperation();
    }
}
