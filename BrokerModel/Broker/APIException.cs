using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Broker
{
    public class APIException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public APIException(string message) : base(message) { }
    }
}
