using Popgun.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Popgun.Net;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;

namespace Popgun.Screens
{
    public class GameplayScreen : GameScreen
    {
        private ISocket Socket;

        /// <summary>
        /// Used for server discovery, it sends if Host is false, and listens if Host is true.
        /// The architecture is as follows, clients will send a broadcast packet to the network, and all available servers will answer with their IP
        /// </summary>
        private UdpClient UdpClient;

        /// <summary>
        /// The IP of the server if we are a client
        /// </summary>
        private IPAddress ServerIP;

        private object EnemyScoreLock = new object();

        private Player Player;
        // The topmost and leftmost bubble
        private Bubble BubbleGraph;
        private List<Bubble> AllBubbles = new List<Bubble>();
        private int BubblesSpawnTimer;

        private Image BackgroundImage;

        private Image TimerImage;
        private Image ScoreImage;
        private Image ScoreNumberImage;
        private Image EnemyScoreImage;
        private Image EnemyScoreNumberImage;
        private Image MultiplayerWaitImage;
        private Image GameOverImage;
        private bool GameOver;
        private Rectangle ScreenBounds;
        private bool Host;
        private bool Multiplayer;

        private long TimeOutLimit;
        private long TimeOutTimer;
        private String PreviousTimerString = String.Empty;

        /// <summary>
        /// Holds actions to be executed on the next update tick
        /// </summary>
        private ConcurrentQueue<Action> ExecutionQueue = new ConcurrentQueue<Action>();

        /// <summary>
        /// The score of the opponent player when in a Multiplayer session
        /// </summary>
        private int EnemyScore;

        public const String BroadcastHeader = "BBSHOT";
        public const String BroadcastSearchRequest = "SEARCH";
        public const String BroadcastSearchResponse = "RESPONSE";

        public const String GamePacketHeader = "POPGUN";
        /// <summary>
        /// Determines whether new bubble rows will spawn based on a timer or based on a missed player shot
        /// </summary>
        public const bool TimeBasedBubbleSpawn = true;

        public const int DefaultGamePort = 5556;
        public const int DefaultBroadcastPort = 5555;
        public const int NumHorizontalBubbles = 19;
        public const int NumVerticalBubbles = 3;

        public const int BubbleHorizontalOffset = 5;
        public const int BubbleVerticalOffset = 2;
        public const int OddBubbleOffset = 18;

        /// <summary>
        /// Time to wait before a new bubble row appears
        /// </summary>
        public const int NewBubblesSpawnTime = 10000;
        
        /// <summary>
        /// The minimum number of bubbles that have to be stacked together to disappear
        /// </summary>
        public const int MinDestroyedBubbles = 3;

        public static Vector2 DefaultFirstBubblePosition = new Vector2(17, 40);
        public static Vector2 DefaultScorePosition = new Vector2(700, 15);
        public static Vector2 DefaultEnemyScorePosition = new Vector2(40, 15);
        public static Vector2 DefaultTimerPosition = new Vector2(20, 550);

        public static Random Random = new Random(12345); // Constant seed

        /// <summary>
        /// Transverses the bubbles graph using depth-first search and performs action in each of them
        /// </summary>
        /// <param name="action">The action to perform on each bubble, if the return value is true then it won't continue transversing that branch</param>
        /// <param name="starting">The starting vertice of the depth first search</param>
        private void PerformBubblesAction(Func<Bubble, bool> action, Bubble starting = null)
        {
            if (starting == null)
            {
                // Perform the action on all bubbles instead of just this one branch (copy the list, as it might be modified in the Action)
                foreach (var bub in AllBubbles.ToList())
                    if (action(bub))
                        break;

                return;
            }

            Stack<Bubble> openNodes = new Stack<Bubble>();
            Stack<Bubble> closedNodes = new Stack<Bubble>();

            openNodes.Push(starting);

            while (openNodes.Count > 0)
            {
                Bubble current = openNodes.Pop();
                if (closedNodes.Contains(current))
                    continue;

                closedNodes.Push(current);

                if (action(current))
                    continue;

                // Push the known neighboring connections
                for (int i = Bubble.TopLeft; i <= Bubble.BottomRight; ++i)
                    if (current.Neighbors[i] != null)
                        openNodes.Push(current.Neighbors[i]);

                // Now push the unknown connections
                foreach (var unknown in current.UnknownConnections)
                    openNodes.Push(unknown);
            }
        }

        public override void SetParameter(String param)
        {
            Host = param.Equals("multihost");
            Multiplayer = Host || param.Contains("multijoin");

            if (Multiplayer && !Host)
                ServerIP = IPAddress.Parse(param.Substring(10));

            if (!Multiplayer)
            {
                TimeOutLimit = long.Parse(param) * 60 * 1000;
                TimeOutTimer = TimeOutLimit;
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            ScreenBounds = new Rectangle(0, 0, (int)ScreenManager.Instance.Dimensions.X, (int)ScreenManager.Instance.Dimensions.Y);
            Player = new Player();
            Player.LoadContent();
            ScoreImage = new Image("", DefaultScorePosition, Vector2.One, text: "Score: ", fontName: "Fonts/Arial");
            ScoreImage.Color = Color.Black;
            ScoreImage.LoadContent();
            EnemyScoreImage = new Image("", DefaultEnemyScorePosition, Vector2.One, text: "Enemy Score: ", fontName: "Fonts/Arial");
            EnemyScoreImage.Color = Color.Black;
            EnemyScoreImage.LoadContent();
            UpdateScoreDisplay();
            BubblesSpawnTimer = NewBubblesSpawnTime;
            GenerateBubbles();
            GameOver = false;
            GameOverImage = new Image("Screens/GameOver", DefaultScorePosition, Vector2.One);
            GameOverImage.LoadContent();
            GameOverImage.Position = new Vector2((ScreenManager.Instance.Dimensions.X - GameOverImage.SourceRect.Width) / 2, ScreenManager.Instance.Dimensions.Y / 2);

            BackgroundImage = new Image("Screens/Gameplay/Background", Vector2.Zero);
            BackgroundImage.LoadContent();

            UpdateTimer();

            // Initialize the socket
            if (Multiplayer)
            {
                EnemyScore = 0;
                if (Host)
                {
                    MultiplayerWaitImage = new Image("", Vector2.Zero, Vector2.One, text: "Waiting for the other player...");
                    MultiplayerWaitImage.LoadContent();
                    // Position the image in the center of the screen
                    MultiplayerWaitImage.Position = new Vector2((ScreenManager.Instance.Dimensions.X - MultiplayerWaitImage.SourceRect.Width) / 2, ScreenManager.Instance.Dimensions.Y / 2);
                    try
                    {
                        Socket = new ServerSocket(IPAddress.Any, DefaultGamePort);
                        Socket.PacketReceived += PacketReceivedHandler;
                        UdpClient = new UdpClient(DefaultBroadcastPort);
                        UdpClient.EnableBroadcast = true;
                        UdpClient.BeginReceive(ReceivedDatagram, null);
                    }
                    catch (SocketException)
                    {
                    }
                }
                else
                {
                    try
                    {
                        Socket = new ClientSocket(ServerIP, DefaultGamePort);
                        Socket.PacketReceived += PacketReceivedHandler;
                    }
                    catch (SocketException)
                    {
                        ScreenManager.Instance.ChangeScreen("TitleScreen");
                    }
                }

                Socket.ErrorHandler += (sock) =>
                {
                    ExecutionQueue.Enqueue(() =>
                    {
                        ScreenManager.Instance.ChangeScreen("TitleScreen");
                    });
                };
            }
        }

        private void UpdateTimer()
        {
            String timerText = String.Format("{0}:{1}", Math.Floor((double)(TimeOutTimer / 1000 / 60)), ((TimeOutTimer / 1000) % 60));
            
            if (timerText == PreviousTimerString)
                return;

            if (TimerImage != null)
                TimerImage.UnloadContent();

            TimerImage = new Image("", DefaultTimerPosition, Vector2.One, text: timerText);
            TimerImage.Color = Color.Black;
            TimerImage.LoadContent();

            PreviousTimerString = timerText;
        }

        /// <summary>
        /// Handles all received network packets in a multiplayer session
        /// </summary>
        /// <param name="packet"></param>
        private void PacketReceivedHandler(ISocket socket, Packet packet)
        {
            switch (packet.Opcode)
            {
                case Opcodes.UpdateScore:
                    ExecutionQueue.Enqueue(() =>
                    {
                        lock (EnemyScoreLock)
                        {
                            EnemyScore = (int)packet.ReadUInt();
                            UpdateScoreDisplay();
                        }
                    });
                    break;
                case Opcodes.BubblesDestroyed:
                    int num = (int)packet.ReadUInt();
                    ExecutionQueue.Enqueue(() =>
                    {
                        SpawnNewBubbles(num);
                    });
                    break;
                case Opcodes.GameEnded:
                    uint result = packet.ReadUInt();
                    ExecutionQueue.Enqueue(() =>
                    {
                        GameOver = true;
                        if (result == 0) // I win
                        {
                            GameOverImage.UnloadContent();
                            GameOverImage = new Image("", DefaultScorePosition, Vector2.One, text: "You win");
                            GameOverImage.LoadContent();
                            GameOverImage.Position = new Vector2((ScreenManager.Instance.Dimensions.X - GameOverImage.SourceRect.Width) / 2, ScreenManager.Instance.Dimensions.Y / 2);
                        }
                    });
                    break;
            }
        }

        /// <summary>
        /// Handles all broadcast requests coming from clients and answers them
        /// </summary>
        /// <param name="result"></param>
        private void ReceivedDatagram(IAsyncResult result)
        {
            try
            {
                // Don't answer if we're already on a session
                if (Socket.Connected)
                    return;

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = UdpClient.EndReceive(result, ref endPoint);
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (BinaryReader sr = new BinaryReader(ms))
                    {
                        // Check the header of the packet
                        if (sr.ReadBytes(Encoding.UTF8.GetByteCount(BroadcastHeader)).SequenceEqual(Encoding.UTF8.GetBytes(BroadcastHeader)))
                        {
                            // Check if this is actually a search request
                            if (sr.ReadBytes(Encoding.UTF8.GetByteCount(BroadcastSearchRequest)).SequenceEqual(Encoding.UTF8.GetBytes(BroadcastSearchRequest)))
                            {
                                // Send the response
                                byte[] response = new byte[Encoding.UTF8.GetByteCount(BroadcastHeader) + Encoding.UTF8.GetByteCount(BroadcastSearchResponse)];
                                using (BinaryWriter br = new BinaryWriter(new MemoryStream(response)))
                                {
                                    br.Write(Encoding.UTF8.GetBytes(BroadcastHeader));
                                    br.Write(Encoding.UTF8.GetBytes(BroadcastSearchResponse));
                                }

                                UdpClient.BeginSend(response, response.Length, endPoint, (asyncRes) => UdpClient.EndSend(asyncRes), null);
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

        /// <summary>
        /// Updates the score number in the HUD
        /// </summary>
        private void UpdateScoreDisplay()
        {
            if (ScoreNumberImage != null)
                ScoreNumberImage.UnloadContent();

            ScoreNumberImage = new Image("", new Vector2(DefaultScorePosition.X + ScoreImage.SourceRect.Width + 10, DefaultScorePosition.Y), Vector2.One, text: Player.Score.ToString(), fontName: "Fonts/Arial");
            ScoreNumberImage.Color = Color.Black;
            ScoreNumberImage.LoadContent();

            lock (EnemyScoreLock)
            {
                if (EnemyScoreNumberImage != null)
                    EnemyScoreNumberImage.UnloadContent();

                if (Multiplayer)
                {
                    EnemyScoreNumberImage = new Image("", new Vector2(DefaultEnemyScorePosition.X + EnemyScoreImage.SourceRect.Width + 10, DefaultEnemyScorePosition.Y), Vector2.One, text: EnemyScore.ToString(), fontName: "Fonts/Arial");
                    EnemyScoreNumberImage.Color = Color.Black;
                    EnemyScoreNumberImage.LoadContent();
                }
            }
        }

        /// <summary>
        /// Recursively creates a row of bubbles to the right
        /// </summary>
        /// <param name="leftBubble">The starting (previous) bubble</param>
        /// <param name="amount">The number of bubbles to create to the right</param>
        /// <returns>The newly created right bubble</returns>
        private Bubble NextRightBubble(Bubble leftBubble, int amount)
        {
            if (amount <= 0)
                return null;

            Bubble bubble = new Bubble(new Vector2(leftBubble.Position.X + leftBubble.GetRectangle().Width + BubbleHorizontalOffset, leftBubble.Position.Y));
            bubble.Offsetted = leftBubble.Offsetted;

            bubble.Neighbors[Bubble.Left] = leftBubble;
            bubble.Neighbors[Bubble.TopLeft] = leftBubble.Neighbors[Bubble.TopRight];

            if (leftBubble.Neighbors[Bubble.TopRight] != null)
            {
                leftBubble.Neighbors[Bubble.TopRight].Neighbors[Bubble.BottomRight] = bubble;
                bubble.Neighbors[Bubble.TopRight] = leftBubble.Neighbors[Bubble.TopRight].Neighbors[Bubble.Right];
                if (leftBubble.Neighbors[Bubble.TopRight].Neighbors[Bubble.Right] != null)
                    leftBubble.Neighbors[Bubble.TopRight].Neighbors[Bubble.Right].Neighbors[Bubble.BottomLeft] = bubble;
            }

            bubble.Neighbors[Bubble.BottomLeft] = leftBubble.Neighbors[Bubble.BottomRight];
            if (leftBubble.Neighbors[Bubble.BottomRight] != null)
            {
                leftBubble.Neighbors[Bubble.BottomRight].Neighbors[Bubble.TopLeft] = bubble;
                bubble.Neighbors[Bubble.BottomRight] = leftBubble.Neighbors[Bubble.BottomRight].Neighbors[Bubble.Right];
                if (bubble.Neighbors[Bubble.BottomRight] != null)
                    bubble.Neighbors[Bubble.BottomRight].Neighbors[Bubble.TopLeft] = bubble;
            }

            bubble.Neighbors[Bubble.Right] = NextRightBubble(bubble, amount - 1);
            AllBubbles.Add(bubble);
            return bubble;
        }

        /// <summary>
        /// Generates the bubble graph
        /// </summary>
        private void GenerateBubbles()
        {
            // Generate the first row
            BubbleGraph = new Bubble(DefaultFirstBubblePosition);
            AllBubbles.Add(BubbleGraph);
            BubbleGraph.Offsetted = false;
            BubbleGraph.Neighbors[Bubble.TopLeft] = null;
            BubbleGraph.Neighbors[Bubble.TopRight] = null;
            BubbleGraph.Neighbors[Bubble.Left] = null;
            BubbleGraph.Neighbors[Bubble.BottomLeft] = null;
            BubbleGraph.Neighbors[Bubble.BottomRight] = null; // Will be filled later in NextRightBubble when building the lower levels
            BubbleGraph.Neighbors[Bubble.Right] = NextRightBubble(BubbleGraph, NumHorizontalBubbles);

            Bubble tempGraph = BubbleGraph;
            // Generate the next N rows
            for (int i = 0; i < NumVerticalBubbles; ++i)
            {
                bool offseted = tempGraph.Offsetted ^ true;
                Bubble nextRow = new Bubble(new Vector2(BubbleGraph.Position.X + (offseted ? OddBubbleOffset : 0), tempGraph.Position.Y + tempGraph.GetRectangle().Height + BubbleVerticalOffset));
                AllBubbles.Add(nextRow);
                nextRow.Offsetted = offseted;

                int rightBubbles = NumHorizontalBubbles;

                // Every second row is offset by some value, so the relationships between bubbles change a bit
                if (offseted)
                {
                    tempGraph.Neighbors[Bubble.BottomRight] = nextRow;
                    nextRow.Neighbors[Bubble.TopLeft] = tempGraph;
                    nextRow.Neighbors[Bubble.TopRight] = tempGraph.Neighbors[Bubble.Right];
                    if (tempGraph.Neighbors[Bubble.Right] != null)
                        tempGraph.Neighbors[Bubble.Right].Neighbors[Bubble.BottomLeft] = nextRow;
                    rightBubbles = NumHorizontalBubbles - 1;
                }
                else
                {
                    tempGraph.Neighbors[Bubble.BottomLeft] = nextRow;
                    nextRow.Neighbors[Bubble.TopRight] = tempGraph;
                    nextRow.Neighbors[Bubble.TopLeft] = tempGraph.Neighbors[Bubble.Left];
                    if (tempGraph.Neighbors[Bubble.Left] != null)
                        tempGraph.Neighbors[Bubble.Left].Neighbors[Bubble.BottomRight] = nextRow;
                    rightBubbles = NumHorizontalBubbles;
                }

                nextRow.Neighbors[Bubble.Left] = null;
                nextRow.Neighbors[Bubble.Right] = NextRightBubble(nextRow, rightBubbles);
                
                // Now move on to the next row
                tempGraph = nextRow;
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            Player.UnloadContent();
            BackgroundImage.UnloadContent();

            if (UdpClient != null)
                UdpClient.Close();
            if (Socket != null)
                Socket.Dispose();

            if (MultiplayerWaitImage != null)
                MultiplayerWaitImage.UnloadContent();

            if (TimerImage != null)
                TimerImage.UnloadContent();

            ScoreImage.UnloadContent();
            ScoreNumberImage.UnloadContent();

            if (EnemyScoreImage != null)
                EnemyScoreImage.UnloadContent();
            if (EnemyScoreNumberImage != null)
                EnemyScoreNumberImage.UnloadContent();

            PerformBubblesAction((bubble) => 
            {
                bubble.UnloadContent();
                return false;
            });
        }

        public static Dictionary<int, int> TransitiveRelations = new Dictionary<int, int> {
                                                              { Bubble.Left, Bubble.Right },
                                                              { Bubble.Right, Bubble.Left },
                                                              { Bubble.TopLeft, Bubble.BottomRight },
                                                              { Bubble.BottomRight, Bubble.TopLeft },
                                                              { Bubble.TopRight, Bubble.BottomLeft },
                                                              { Bubble.BottomLeft, Bubble.TopRight },
                                                          };

        private void SetTransitiveRelation(Bubble one, Bubble other, int directionOne)
        {
            if (one == null || other == null)
                return;

            one.Neighbors[directionOne] = other;
            other.Neighbors[TransitiveRelations[directionOne]] = one;
        }

        private void SetRelations(Bubble one, Bubble bubble, int direction)
        {
            if (one == null || bubble == null)
                return;

            switch (direction)
            {
                case Bubble.TopLeft:
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.TopRight], Bubble.Right);
                    SetTransitiveRelation(one, bubble, Bubble.BottomRight);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.Left], Bubble.BottomLeft);
                    if (one.Neighbors[Bubble.BottomLeft] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.BottomLeft].Neighbors[Bubble.TopLeft], Bubble.Left);
                    if (bubble.Neighbors[Bubble.TopRight] != null)
                        SetTransitiveRelation(one, bubble.Neighbors[Bubble.TopRight].Neighbors[Bubble.TopLeft], Bubble.TopRight);
                    if (one.Neighbors[Bubble.Left] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Left].Neighbors[Bubble.TopRight], Bubble.TopLeft);
                    break;
                case Bubble.TopRight:
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.TopLeft], Bubble.Left);
                    SetTransitiveRelation(one, bubble, Bubble.BottomLeft);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.Right], Bubble.BottomRight);
                    if (one.Neighbors[Bubble.BottomRight] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.BottomRight].Neighbors[Bubble.TopRight], Bubble.Right);
                    if (one.Neighbors[Bubble.Left] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Left].Neighbors[Bubble.TopRight], Bubble.TopLeft);
                    if (one.Neighbors[Bubble.Right] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Right].Neighbors[Bubble.TopLeft], Bubble.TopRight);
                    break;
                case Bubble.Left:
                    SetTransitiveRelation(one, bubble, Bubble.Right);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.TopLeft], Bubble.TopRight);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.BottomLeft], Bubble.BottomRight);
                    if (one.Neighbors[Bubble.TopRight] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopRight].Neighbors[Bubble.Left], Bubble.TopLeft);
                    if (one.Neighbors[Bubble.TopLeft] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopLeft].Neighbors[Bubble.BottomLeft], Bubble.Left);
                    if (one.Neighbors[Bubble.Left] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Left].Neighbors[Bubble.BottomRight], Bubble.BottomLeft);
                    break;
                case Bubble.Right:
                    SetTransitiveRelation(one, bubble, Bubble.Left);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.TopRight], Bubble.TopLeft);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.BottomRight], Bubble.BottomLeft);
                    if (one.Neighbors[Bubble.TopLeft] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopLeft].Neighbors[Bubble.Right], Bubble.TopRight);
                    if (one.Neighbors[Bubble.TopRight] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopRight].Neighbors[Bubble.BottomRight], Bubble.Right);
                    if (one.Neighbors[Bubble.Right] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Right].Neighbors[Bubble.BottomLeft], Bubble.BottomRight);
                    break;
                case Bubble.BottomLeft:
                    SetTransitiveRelation(one, bubble, Bubble.TopRight);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.Left], Bubble.TopLeft);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.BottomRight], Bubble.Right);
                    if (one.Neighbors[Bubble.TopLeft] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopLeft].Neighbors[Bubble.BottomLeft], Bubble.Left);
                    if (one.Neighbors[Bubble.Left] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Left].Neighbors[Bubble.BottomRight], Bubble.BottomLeft);
                    if (one.Neighbors[Bubble.Right] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Right].Neighbors[Bubble.BottomLeft], Bubble.BottomRight);
                    break;
                case Bubble.BottomRight:
                    SetTransitiveRelation(one, bubble, Bubble.TopLeft);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.Right], Bubble.TopRight);
                    SetTransitiveRelation(one, bubble.Neighbors[Bubble.BottomLeft], Bubble.Left);
                    if (one.Neighbors[Bubble.TopRight] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.TopRight].Neighbors[Bubble.BottomRight], Bubble.Right);
                    if (one.Neighbors[Bubble.Left] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Left].Neighbors[Bubble.BottomRight], Bubble.BottomLeft);
                    if (one.Neighbors[Bubble.Right] != null)
                        SetTransitiveRelation(one, one.Neighbors[Bubble.Right].Neighbors[Bubble.BottomLeft], Bubble.BottomRight);
                    break;
            }
        }

        private void RemoveFromGraph(Bubble bubble, bool clearRelations = true)
        {
            bubble.Visible = false;
            if (clearRelations)
            {
                for (int i = Bubble.TopLeft; i <= Bubble.BottomRight; ++i)
                {
                    if (bubble.Neighbors[i] != null)
                    {
                        for (int j = Bubble.TopLeft; j <= Bubble.BottomRight; ++j)
                        {
                            if (bubble.Neighbors[i].Neighbors[j] == bubble)
                            {
                                bubble.Neighbors[i].Neighbors[j] = null;
                            }
                        }
                    }

                    bubble.Neighbors[i] = null;
                }
                bubble.UnknownConnections.Clear();
                PerformBubblesAction((bb) =>
                {
                    if (bb.UnknownConnections.Contains(bubble))
                        bb.UnknownConnections.Remove(bubble);
                    return false;
                });
            }
            AllBubbles.Remove(bubble);
            bubble.UnloadContent();
        }

        /// <summary>
        /// Sends our new score to the other player
        /// </summary>
        private void SendScoreUpdate()
        {
            Packet packet = new Packet(Opcodes.UpdateScore, 4);
            packet.WriteUInt((uint)Player.Score);
            Socket.Send(packet);
        }

        /// <summary>
        /// Notifies the other player that some bubbles have been destroyed
        /// </summary>
        private void SendBubblesDestroyed(uint num)
        {
            Packet packet = new Packet(Opcodes.BubblesDestroyed, 4);
            packet.WriteUInt(num);
            Socket.Send(packet);
        }

        private void HandleBulletCollision(Bubble bullet, Bubble collisioned)
        {
            // The bullet has collided with the graph
            bullet.Velocity.Normalize();
            
            // Backtrack a little so that we stop intersecting, in order to smooth out the direction detection process
            while (collisioned.GetRectangle().Intersects(bullet.GetRectangle()))
                bullet.Position -= bullet.Velocity * 0.1f;

            // Stop the bullet
            bullet.Velocity = Vector2.Zero;

            // Add the bullet to the list of bubbles
            AllBubbles.Add(bullet);

            // Now add the bullet to the graph, we first have to calculate the correct direction
            int direction = collisioned.GetContactDirection(bullet);
            if (collisioned.Neighbors[direction] == null)
            {
                collisioned.Neighbors[direction] = bullet;
                // Adjust the bubble position based on the direction it hit
                var newPosition = bullet.Position;
                switch (direction)
                {
                    case Bubble.TopLeft:
                        newPosition.X = collisioned.Position.X - bullet.GetRectangle().Width - BubbleHorizontalOffset + OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y - bullet.GetRectangle().Height - BubbleVerticalOffset;
                        break;
                    case Bubble.TopRight:
                        newPosition.X = collisioned.Position.X + collisioned.GetRectangle().Width + BubbleHorizontalOffset - OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y - bullet.GetRectangle().Height - BubbleVerticalOffset;
                        break;
                    case Bubble.Left:
                        newPosition.X = collisioned.Position.X - bullet.GetRectangle().Width - BubbleHorizontalOffset + OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y;
                        break;
                    case Bubble.Right:
                        newPosition.X = collisioned.Position.X + collisioned.GetRectangle().Width + BubbleHorizontalOffset - OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y;
                        break;
                    case Bubble.BottomLeft:
                        newPosition.X = collisioned.Position.X - bullet.GetRectangle().Width - BubbleHorizontalOffset + OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y + collisioned.GetRectangle().Height + BubbleVerticalOffset;
                        break;
                    case Bubble.BottomRight:
                        newPosition.X = collisioned.Position.X + collisioned.GetRectangle().Width + BubbleHorizontalOffset - OddBubbleOffset;
                        newPosition.Y = collisioned.Position.Y + collisioned.GetRectangle().Height + BubbleVerticalOffset;
                        break;
                }
                bullet.Position = newPosition;
                // Now update the other adjacent bubbles
                SetRelations(bullet, collisioned, direction);
            }
            else
            {
                // Swap the two nodes
                if (collisioned.Neighbors[direction].Visible == false)
                {
                    var oldNode = collisioned.Neighbors[direction];
                    
                    // Update all the node references
                    for (int i = Bubble.TopLeft; i <= Bubble.BottomRight; ++i)
                    {
                        bullet.Neighbors[i] = collisioned.Neighbors[direction].Neighbors[i];
                        SetRelations(collisioned.Neighbors[direction].Neighbors[i], bullet.Neighbors[i], i);
                    }

                    RemoveFromGraph(oldNode, false);
                }
                else
                {
                    // If no direction could be calculated (or the calculated one is incorrect) then add  the new bubble to a special list to maintain connectivity
                    collisioned.UnknownConnections.Add(bullet);
                    bullet.Neighbors[bullet.GetContactDirection(collisioned)] = collisioned;
                }
            }

            // Now that the bullet is part of the graph, perform a DFS starting from the newly added bubble and check for same-color adjacent bubbles
            List<Bubble> adjacents = new List<Bubble>();
            PerformBubblesAction((b) =>
            {
                // We found a same-color bubble so we add it to the list and continue the search
                if (b.Visible && b.ColorIndex == bullet.ColorIndex)
                {
                    adjacents.Add(b);
                    return false;
                }

                // We found a dead end, stop searching this branch and proceed to the others
                return true;

            }, bullet);

            // Now hide all the matching adjacent bubbles
            if (adjacents.Count >= MinDestroyedBubbles)
            {
                foreach (Bubble b in adjacents)
                {
                    b.Visible = false;
                    RemoveFromGraph(b);
                }

                Player.Score += adjacents.Count - MinDestroyedBubbles + 1;

                UpdateScoreDisplay();

                // If we're in a multiplayer session, then notify the other player of our score
                if (Multiplayer)
                {
                    SendBubblesDestroyed((uint)adjacents.Count);
                    SendScoreUpdate();
                }

                // Destroy the disconnected bubbles
                PerformBubblesAction((bubble) =>
                {
                    // Don't make it disappear if there's at least one connection visible.

                    for (int i = Bubble.TopLeft; i <= Bubble.BottomRight; ++i)
                        if (bubble.Neighbors[i] != null)
                            if (bubble.Neighbors[i].Visible)
                                return false;
                    
                    foreach (var bub in bubble.UnknownConnections)
                        if (bub.Visible)
                            return false;

                    bubble.Velocity.Y = 0.9f;
                    //bubble.Visible = false;
                    return false;
                });
            }
            else
            {
                // If we are supposed to spawn bubbles only when the player missed the shot, do it here
                if (!TimeBasedBubbleSpawn)
                    SpawnNewBubbles();
            }
        }

        /// <summary>
        /// Ends the game and notifies the other player in the case of a multiplayer session
        /// </summary>
        private void EndGame()
        {
            if (GameOver)
                return;

            GameOver = true;
            if (Multiplayer)
            {
                Packet packet = new Packet(Opcodes.GameEnded, 4);
                packet.WriteUInt(0); // I lose
                Socket.Send(packet);
            }
            else
            {
                // Ask the user for its name and then save the highscore
                var form = new HighScoreSaveForm(Player.Score, (int)(TimeOutLimit / 60 / 1000));
                form.ShowDialog();
                ScreenManager.Instance.ChangeScreen("TitleScreen");
            }
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

            if (GameOver)
            {
                GameOverImage.Update(time);
                return;
            }

            // If we are in a multiplayer session, wait until the socket is connected.
            if (Multiplayer && Host && Socket != null && !Socket.Connected)
            {
                MultiplayerWaitImage.Update(time);
                return;
            }

            BackgroundImage.Update(time);

            if (TimeOutLimit != 0)
            {
                TimeOutTimer -= time.ElapsedGameTime.Milliseconds;
                if (TimeOutTimer <= 0)
                {
                    EndGame();
                    return;
                }
            }

            UpdateTimer();

            Player.Update(time);

            // If the player's bullet is outside bounds, destroy it
            if (Player.Bullet != null)
            {
                if (ScreenBounds.Intersects(Player.Bullet.GetRectangle()))
                {
                    if (Player.Bullet.Position.X + Player.Bullet.GetRectangle().Width >= ScreenBounds.X + ScreenBounds.Width || Player.Bullet.Position.X <= ScreenBounds.X)
                        Player.Bullet.Velocity.X *= -1;
                    if (Player.Bullet.Position.Y + Player.Bullet.GetRectangle().Height >= ScreenBounds.Y + ScreenBounds.Height || Player.Bullet.Position.Y <= ScreenBounds.Y)
                        Player.Bullet.Velocity.Y *= -1;
                }
                else if (!ScreenBounds.Contains(Player.Bullet.GetRectangle()))
                {
                    Player.Bullet.UnloadContent();
                    Player.Bullet = null;
                }
            }

            PerformBubblesAction((bubble) => 
            {
                // If the bubbles have gotten past the player, the game is over
                if (bubble.Position.Y + bubble.GetRectangle().Height > Player.Position.Y)
                {
                    // Bullets with velocity are actually falling, don't end the game for those
                    if (bubble.Velocity == Vector2.Zero && bubble.Visible)
                    {
                        EndGame();
                        return true;
                    }
                }

                if (!ScreenBounds.Contains(bubble.GetRectangle()))
                {
                    // Stop it and remove it from the graph
                    bubble.Velocity = Vector2.Zero;
                    RemoveFromGraph(bubble);
                    return true;
                }

                if (!bubble.Visible)
                    return false;

                bubble.Update(time);
                if (Player.Bullet != null)
                {
                    if (Player.Bullet.Intersects(bubble))
                    {
                        HandleBulletCollision(Player.Bullet, bubble);
                        Player.Bullet = null;
                    }
                }
                return false;
            });

            if (TimeBasedBubbleSpawn)
            {
                // Update the timer and add new bubbles if needed
                BubblesSpawnTimer -= time.ElapsedGameTime.Milliseconds;
                if (BubblesSpawnTimer <= 0)
                {
                    SpawnNewBubbles();
                    BubblesSpawnTimer = NewBubblesSpawnTime;
                }
            }
        }

        /// <summary>
        /// Creates a new row of bubbles at the top of the graph and updates the initial bubble
        /// </summary>
        private void SpawnNewBubbles(int num = NumHorizontalBubbles)
        {
            Bubble nextRow = new Bubble(new Vector2(BubbleGraph.Position.X + (BubbleGraph.Offsetted ? -OddBubbleOffset : OddBubbleOffset), BubbleGraph.Position.Y));
            nextRow.Offsetted = BubbleGraph.Offsetted ^ true;

            // Make all the bubbles go down, do this before adding relationships to our new bubble
            PerformBubblesAction((bubble) =>
            {
                var pos = bubble.Position;
                pos.Y += bubble.GetRectangle().Height + BubbleVerticalOffset;
                bubble.Position = pos;
                return false;
            });

            AllBubbles.Add(nextRow);

            int rightBubbles = num;

            if (nextRow.Offsetted)
            {
                BubbleGraph.Neighbors[Bubble.TopRight] = nextRow;
                nextRow.Neighbors[Bubble.BottomLeft] = BubbleGraph;
                nextRow.Neighbors[Bubble.BottomRight] = BubbleGraph.Neighbors[Bubble.Right];
                if (BubbleGraph.Neighbors[Bubble.Right] != null)
                    BubbleGraph.Neighbors[Bubble.Right].Neighbors[Bubble.TopLeft] = nextRow;

                rightBubbles = num - 1;
            }
            else
            {
                BubbleGraph.Neighbors[Bubble.TopLeft] = nextRow;
                nextRow.Neighbors[Bubble.BottomRight] = BubbleGraph;
                nextRow.Neighbors[Bubble.BottomLeft] = BubbleGraph.Neighbors[Bubble.Left];
                if (BubbleGraph.Neighbors[Bubble.Left] != null)
                    BubbleGraph.Neighbors[Bubble.Left].Neighbors[Bubble.TopRight] = nextRow;

                rightBubbles = num;
            }

            nextRow.Neighbors[Bubble.Right] = NextRightBubble(nextRow, rightBubbles);
            
            // Update the top of the graph
            BubbleGraph = nextRow;
        }

        public override void Draw(SpriteBatch batch)
        {
            // Execute all pending tasks
            while (!ExecutionQueue.IsEmpty)
            {
                Action action;
                if (ExecutionQueue.TryDequeue(out action))
                    action();
            }

            if (GameOver)
            {
                GameOverImage.Draw(batch);
                return;
            }

            if (Multiplayer && Host && Socket != null && !Socket.Connected)
            {
                if (MultiplayerWaitImage != null)
                    MultiplayerWaitImage.Draw(batch);
                return;
            }

            BackgroundImage.Draw(batch);

            TimerImage.Draw(batch);

            // Now use DFS to transverse the graph and draw each Bubble
            PerformBubblesAction((bubble) =>
            {
                bubble.Draw(batch); 
                return false;
            });

            ScoreImage.Draw(batch);
            ScoreNumberImage.Draw(batch);
            
            if (Multiplayer)
            {
                EnemyScoreImage.Draw(batch);
                EnemyScoreNumberImage.Draw(batch);
            }

            Player.Draw(batch);
        }
    }
}
