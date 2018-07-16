using Shared.Interfaces;
using System;

namespace Shared.Common
{
    public class ConsoleLogger : ILogger
    {
        void ILogger.Log(LogPriority priority, string message, string name)
        {
            Console.WriteLine($"{priority.ToString()} | {message}");
        }
    }
}
