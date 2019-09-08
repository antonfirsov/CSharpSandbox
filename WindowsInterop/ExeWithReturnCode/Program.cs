using System;

namespace ExeWithReturnCode
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Arg count is: "+ args.Length);
            return args.Length;
        }
    }
}