using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces
{
    public interface IDataStream
    {
        Task Start(ParseMessage parsemsg);
        Task Stop();
        Task SendCommand(byte[] bytes);
        bool IsConnected { get; }
    }

    public delegate Task ParseMessage(SocketEventHandlerArgs msg);



}
