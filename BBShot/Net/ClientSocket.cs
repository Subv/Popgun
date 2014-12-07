using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Popgun.Net
{
    public class ClientSocket : ISocket
    {
        public PacketReceivedDelegate PacketReceived { get; set; }
        public ErrorDelegate ErrorHandler { get; set; }
        private Socket socket;
        private byte[] ReceiveBuffer = new byte[700];
        private byte[] SendBuffer = new byte[700];

        public bool Connected
        {
            get { return socket.Connected; }
        }

        public ClientSocket(IPAddress ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(new IPEndPoint(ip, port), new AsyncCallback(HandleConnected), null);
        }

        private void HandleConnected(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);
                socket.BeginReceive(ReceiveBuffer, 0, 700, SocketFlags.None, new AsyncCallback(SocketReceive), null);
            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException)
            {
                if (ErrorHandler != null)
                    ErrorHandler(this);
            }
        }

        private void SocketReceive(IAsyncResult result)
        {
            try
            {
                socket.EndReceive(result);
                Packet packet = new Packet(ReceiveBuffer);
                if (PacketReceived != null)
                    PacketReceived(this, packet);
                ReceiveBuffer = new byte[700];
                socket.BeginReceive(ReceiveBuffer, 0, 700, SocketFlags.None, new AsyncCallback(SocketReceive), null);
            }
            catch (SocketException)
            {
                if (ErrorHandler != null)
                    ErrorHandler(this);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Send(Packet packet)
        {
            try
            {
                if (!socket.Connected)
                    return;
                SocketError errorCode;
                socket.BeginSend(packet.GetData(), 0, packet.GetData().Length, SocketFlags.None, out errorCode, new AsyncCallback(SocketSendFinished), null);
            }
            catch (SocketException)
            {
                if (ErrorHandler != null)
                    ErrorHandler(this);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void SocketSendFinished(IAsyncResult result)
        {
            try
            {
                socket.EndSend(result);
            }
            catch (SocketException)
            {
                if (ErrorHandler != null)
                    ErrorHandler(this);
            }
            catch(ObjectDisposedException)
            {
            }
        }

        public void Dispose()
        {
            socket.Close(0);
        }
    }
}
