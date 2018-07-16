using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces
{
    public enum LogPriority
    {
        Error,
        Warning,
        Info,
        Debug,
        Quotes,
    }

    public interface ILogger
    {
        void Log(LogPriority priority, string message, string source ="");
    }
}
