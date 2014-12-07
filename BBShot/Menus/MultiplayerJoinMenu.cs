using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Popgun.Screens;
using System.IO;
using Popgun.Effects;
using System.Collections.Concurrent;

namespace Popgun.Menus
{
    public class MultiplayerJoinMenu : Menu
    {
        /// <summary>
        /// Holds a list of actions to execute on the next Update tick
        /// </summary>
        [XmlIgnore]
        private ConcurrentQueue<Action> ExecutionQueue;

        [XmlIgnore]
        private UdpClient UdpClient;

        /// <summary>
        /// Frequency of the broadcast query
        /// </summary>
        private const int BroadcastRequestFrequency = 4000;
        
        /// <summary>
        /// If a server hasn't responded within this time, it will be removed from the list
        /// </summary>
        private const int ServerListClearFrequency = 1000;

        /// <summary>
        /// Default port for the UdpClient to listen on, it won't be used since we're doing Broadcast
        /// </summary>
        private const int ClientPort = 1234;

        /// <summary>
        /// The original menu items before any servers are added
        /// </summary>
        [XmlIgnore]
        private List<MenuItem> OriginalItems;

        [XmlIgnore]
        private List<IPEndPoint> Servers;

        [XmlIgnore]
        private bool Waiting;

        [XmlIgnore]
        private int BroadcastRequestTimer;

        [XmlIgnore]
        private int ServerListClearTimer;

        public MultiplayerJoinMenu()
        {
            Id = "Xml/Menus/GamesList.xml";
            Servers = new List<IPEndPoint>();
            ExecutionQueue = new ConcurrentQueue<Action>();
            OriginalItems = new List<MenuItem>();
            BroadcastRequestTimer = BroadcastRequestFrequency;
            ServerListClearTimer = ServerListClearFrequency;
            Waiting = false;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            try
            {
                UdpClient = new UdpClient(ClientPort);
                UdpClient.EnableBroadcast = true;
                UdpClient.BeginReceive(ReceivedDatagram, null);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            // Save the original items
            OriginalItems.AddRange(Items);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            UdpClient.Close();
        }

        private void UpdateDisplay()
        {
            foreach (var item in Items)
                if (!OriginalItems.Contains(item))
                    item.Image.UnloadContent();

            Items.Clear();
            foreach (var server in Servers)
            {
                var item = new MenuItem
                {
                    Image = new Image("", Vector2.Zero, Vector2.One, text: server.Address.ToString(), fontName: "Fonts/Arial"),
                    LinkType = MenuItem.LinkTypes.Screen,
                    LinkID = "GameplayScreen",
                    Parameter = "multijoin;" + server.Address.ToString()
                };

                item.Image.Color = Color.Black;
                item.Image.LoadContent();
                foreach (ImageEffect effect in Effects)
                    item.Image.AddEffect(effect.Clone() as ImageEffect);

                Items.Add(item);
            }

            Items.AddRange(OriginalItems);

            ItemNumber = Math.Min(ItemNumber, Items.Count - 1);
            AlignMenuItems();
        }

        private void ReceivedDatagram(IAsyncResult result)
        {
            try
            {
                IPEndPoint server = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = UdpClient.EndReceive(result, ref server);
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (BinaryReader sr = new BinaryReader(ms))
                    {
                        // Check the header of the packet
                        if (sr.ReadBytes(Encoding.UTF8.GetByteCount(GameplayScreen.BroadcastHeader)).SequenceEqual(Encoding.UTF8.GetBytes(GameplayScreen.BroadcastHeader)))
                        {
                            // Verify if it's a response
                            if (sr.ReadBytes(Encoding.UTF8.GetByteCount(GameplayScreen.BroadcastSearchResponse)).SequenceEqual(Encoding.UTF8.GetBytes(GameplayScreen.BroadcastSearchResponse)))
                            {
                                // Add the server to the list and update the display, it has to be done on the main thread
                                ExecutionQueue.Enqueue(() =>
                                {
                                    Servers.Add(server);
                                    UpdateDisplay();
                                });
                            }
                        }
                    }
                }

                UdpClient.BeginReceive(ReceivedDatagram, null);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private IPAddress GetBroadcast()
        {
            try
            {
                string ipadress;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); // get a list of all local IPs
                
                // Filter IPv4 addresses
                var list = ipHostInfo.AddressList.Where((addr) => addr.AddressFamily == AddressFamily.InterNetwork).ToList();

                IPAddress localIpAddress = list[0]; // choose the first of the list
                ipadress = Convert.ToString(localIpAddress); // convert to string
                ipadress = ipadress.Substring(0, ipadress.LastIndexOf(".") + 1); // cuts of the last octet of the given IP 
                ipadress += "255"; // adds 255 witch represents the local broadcast
                return IPAddress.Parse(ipadress);
            }
            catch (Exception)
            {
                return IPAddress.Parse("127.0.0.1"); // in case of error return the local loopback
            }
        }

        public override void Update(GameTime time)
        {
            // Executing all pending actions
            while (!ExecutionQueue.IsEmpty)
            {
                Action action;
                if (ExecutionQueue.TryDequeue(out action))
                    action();
            }

            base.Update(time);

            BroadcastRequestTimer -= time.ElapsedGameTime.Milliseconds;
            if (BroadcastRequestTimer <= 0)
            {
                // Timer expired, broadcast a server request
                byte[] data = new byte[Encoding.UTF8.GetByteCount(GameplayScreen.BroadcastHeader) + Encoding.UTF8.GetByteCount(GameplayScreen.BroadcastSearchRequest)];
                using (var br = new BinaryWriter(new MemoryStream(data)))
                {
                    br.Write(Encoding.UTF8.GetBytes(GameplayScreen.BroadcastHeader));
                    br.Write(Encoding.UTF8.GetBytes(GameplayScreen.BroadcastSearchRequest));
                }

                Servers.Clear();
                UdpClient.BeginSend(data, data.Length, new IPEndPoint(IPAddress.Broadcast, GameplayScreen.DefaultBroadcastPort), (result) => UdpClient.EndSend(result), null);
                BroadcastRequestTimer = BroadcastRequestFrequency;
                UpdateDisplay();
            }
        }
    }
}
