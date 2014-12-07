using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Popgun.Net
{
    public delegate void PacketReceivedDelegate(ISocket socket, Packet packet);
    public delegate void ErrorDelegate(ISocket socket);

    public interface ISocket : IDisposable
    {
        bool Connected { get; }
        void Send(Packet packet);

        PacketReceivedDelegate PacketReceived { get; set; }
        ErrorDelegate ErrorHandler { get; set; }
    }
}
