using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.ServiceReferences;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            TestServiceClient client = new TestServiceClient("NetTcpBinding_ITestService");
            Console.WriteLine(client.Ping());

            Console.ReadLine();
        }
    }
}
