using System;

namespace ExeWithReturnCode
{
    internal class Program
    {
        /// <summary>
        /// Reports successful exit code (0), if no arguments were passed.
        /// </summary>
        public static int Main(string[] args)
        {
            Console.WriteLine("Arg count is: "+ args.Length);
            return args.Length;
        }
    }
}