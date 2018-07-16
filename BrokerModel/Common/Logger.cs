using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common
{
    public class Log4NetWrapper : ILogger
    {
        private readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Log4NetWrapper));

        public void Log(LogPriority priority, string message, string source = "")
        {
            if (priority == LogPriority.Debug)
            {
                _Log.Debug(message);
            }
            else if (priority == LogPriority.Error)
            {
                _Log.Error(message);
            }
            else if (priority == LogPriority.Info)
            {
                _Log.Info(message);
            }
            else if (priority == LogPriority.Warning)
            {
                _Log.Warn(message);
            }
        }
    }
}
