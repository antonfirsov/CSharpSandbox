using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var svcHost = new ServiceHost(typeof(TestService));
            Console.WriteLine("Available Endpoints :\n");
            svcHost.Description.Endpoints.ToList().ForEach(endpoints => Console.WriteLine(endpoints.Address.ToString()));
            svcHost.Open();
            Console.ReadLine();
        }
    }
}
