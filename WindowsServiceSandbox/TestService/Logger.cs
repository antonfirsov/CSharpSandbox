using System;
using System.IO;

namespace TestService
{
    public interface ILogger
    {
        void Log(string stuff);
    }

    public class Logger : ILogger
    {
        public void Log(string stuff)
        {
            try
            {
                string dir = new FileInfo(GetType().Assembly.Location).DirectoryName;
                string path = Path.Combine(dir, "_log.log");
                File.AppendAllLines(path, new[] { stuff });
            }
            catch (Exception ex)
            {

            }
        }
    }
}