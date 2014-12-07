using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Popgun.Net
{
    public class ServerSocket : ISocket
    {
        private Socket serverSocket;
        private Socket clientSocket;
        public delegate void SocketAcceptedDelegate(Socket socket);
        public ErrorDelegate ErrorHandler { get; set; }
        public PacketReceivedDelegate PacketReceived { get; set; }
        public SocketAcceptedDelegate SocketAccepted;
        private byte[] ReceiveBuffer = new byte[700];

        public bool Connected
        {
            get { return clientSocket != null && clientSocket.Connected; }
        }

        public ServerSocket(IPAddress ip, int port)
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, port));
            serverSocket.Listen(1000);
            BeginAccept();
        }

        public void BeginAccept()
        {
            serverSocket.BeginAccept(new AsyncCallback(SockAccepted), null);
        }

        private void SockAccepted(IAsyncResult result)
        {
            try
            {
                clientSocket = serverSocket.EndAccept(result);
                if (SocketAccepted != null)
                    SocketAccepted(clientSocket);
                SocketError errorCode;
                clientSocket.BeginReceive(ReceiveBuffer, 0, 700, SocketFlags.None, out errorCode, new AsyncCallback(BytesReceivedAsync), null);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void BytesReceivedAsync(IAsyncResult result)
        {
            try
            {
                clientSocket.EndReceive(result);
                Packet received = new Packet(ReceiveBuffer);
                if (PacketReceived != null)
                    PacketReceived(this, received);
                ReceiveBuffer = new byte[700];
                SocketError errorCode;
                clientSocket.BeginReceive(ReceiveBuffer, 0, 700, SocketFlags.None, out errorCode, new AsyncCallback(BytesReceivedAsync), null);
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

        public void Send(Packet packet)
        {
            if (clientSocket == null || !clientSocket.Connected)
                return;
            SocketError errorCode;
            clientSocket.BeginSend(packet.GetData(), 0, packet.GetData().Length, SocketFlags.None, out errorCode, new AsyncCallback(SocketSendFinished), null);
        }

        private void SocketSendFinished(IAsyncResult result)
        {
            clientSocket.EndSend(result);
        }

        public void Dispose()
        {
            serverSocket.Close(0);
            if (clientSocket != null)
                clientSocket.Close();
        }
    }
}
