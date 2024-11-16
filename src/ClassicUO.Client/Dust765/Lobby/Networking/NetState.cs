using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ClassicUO.Dust765.Lobby.Networking.Collections;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public delegate void DisconnectCallback(NetState ns);

    public sealed class NetState
    {
        public static NetState Connect(IPEndPoint ipep)
        {
            if(ipep == null)
                throw new ArgumentNullException("ipep", "Network endpoint required");

            Console.WriteLine($"Connecting to..{ipep.Address}:{ipep.Port}");

            Socket socket = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult result = socket.BeginConnect(ipep, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(1000, false);
            try
            {
                if(success)
                {
                    socket.EndConnect(result);
                    if(socket.Connected)
                        return new NetState(socket);
                } else
                {
                    socket.Close();
                    throw new SocketException((int)SocketError.TimedOut);
                }
            } catch(SocketException se)
            {
                Console.WriteLine($"Connection failed due to socket: {se.SocketErrorCode}");
            } catch(Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.InnerException}");
            }
            return null;
        }

        public static bool ValidateIPv4(string ipString)
        {
            if(String.IsNullOrWhiteSpace(ipString))
                return false;

            string[] splitValues = ipString.Split('.');
            if(splitValues.Length != 4)
                return false;

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        private bool _disconnected;
        private DateTime _connectedAt;

        private Socket _socket; IPAddress _socketAddress;

        byte[] _inputBuffer; InputQueue _inputQueue;
        OutputQueue _outputQueue;

        Mobile _mobile;

        public NetState(Socket socket)
        {
            socket.LingerState.Enabled = false;

            _socket = socket;
            _socketAddress = ( (IPEndPoint)socket.RemoteEndPoint ).Address;
            _connectedAt = DateTime.Now;
            _inputBuffer = new byte[2048];
            _inputQueue = new InputQueue() { Callback = new AsyncCallback(OnInputReceived) };
            _outputQueue = new OutputQueue() { Callback = new AsyncCallback(OnOutput) };

            BeginInputReceive();
        }

        public Socket Client => _socket;
        public IPAddress Address => _socketAddress;
        public DateTime ConnectedAt => _connectedAt;
        public TimeSpan Uptime => _connectedAt - DateTime.Now;
        public bool IsOpen => !_disconnected;

        internal Mobile Mobile
        {
            get => _mobile;
            set
            {
                _mobile = value;
                OnMobileChanged();
            }
        }

        private void OnMobileChanged()
        {
            _inputQueue.Clear();
        }

        private void BeginInputReceive()
        {
            lock(this)
            {
                if(!_disconnected)
                {
                    try
                    {
                        _socket.BeginReceive(_inputBuffer, 0, _inputBuffer.Length, SocketFlags.None, _inputQueue.Callback, null);
                    } catch(Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private void OnInputReceived(IAsyncResult res)
        {
            try
            {
                int length = _socket.EndReceive(res);
                if(length <= 0)
                {
                    Disconnect();
                    return;
                } else
                {
                    lock(_inputQueue)
                    {
                        _inputQueue.Enqueue(_inputBuffer, 0, length);
                    }
                    BeginInputReceive();
                }
            } catch(SocketException se)
            {
                GameActions.Print($"Cheat server is now offline. ({(SocketError)se.SocketErrorCode})");
                Disconnect();
            } catch(ObjectDisposedException)
            {
            } catch(Exception)
            {
                GC.SuppressFinalize(this);
            }
        }

        private void OnOutput(IAsyncResult res)
        {
            try
            {
                int length = _socket.EndSend(res);
                if(length <= 0)
                {
                    Disconnect();
                    return;
                }
                Send(_outputQueue.Proceed());
            } catch(Exception ex)
            {
                OnError(ex);
            }
        }

        public void Send(Packet packet)
        {
            if(packet == null)
                throw new ArgumentNullException("packet");

            if(_disconnected)
                return;
            try
            {
                byte[] buffer = packet.Compile();
                if(buffer.Length > 0)
                {
                    _outputQueue.Enqueue(buffer, 0, buffer.Length);
                    Send(_outputQueue.Query());
                }
            } catch(Exception ex)
            {
                OnError(ex);
            }
        }

        private void Send(OutputQueue.Gram gram)
        {
            if(gram == null)
                return;

            if(_socket != null)
                _socket.BeginSend(gram.Buffer, 0, gram.Count, SocketFlags.None, _outputQueue.Callback, null);
        }

        public void Slice()
        {
            while(!_disconnected && _inputQueue.Count > 0)
            {
                int packetID = _inputQueue.GetPacketID();
                if(packetID <= 0)
                {
                    //PacketReader.Initialize(_inputQueue.Dequeue(_inputQueue.Count), false, (byte)packetID, "Unknown").Trace(true);
                    return;
                }

                PacketHandler handler = PacketHandlers.GetHandler((byte)packetID);
                if(handler == null)
                {
                    handler = new PacketHandler(packetID, null);
                    PacketReader.Initialize(_inputQueue.Dequeue(_inputQueue.Count), handler).Trace(true);
                } else
                {
                    int length;
                    if(( length = _inputQueue.GetPacketLength() ) < 3)
                    {
                        Disconnect();
                        return;
                    }
                    handler.Callback(this, PacketReader.Initialize(_inputQueue.Dequeue(length), handler));
                }
            }
        }

        public DisconnectCallback OnDisconnect { get; set; }

        public void Disconnect()
        {
            if(_disconnected)
                return;

            if(OnDisconnect != null)
                OnDisconnect(this);

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            } catch { }

            try
            {
                _socket.Close();
            } catch { }

            _socket = null;
            _disconnected = true;
            //GC.SuppressFinalize(this);
        }

        private void OnError(Exception ex) => Console.WriteLine($"An error has occured: {ex.Message}");

        public override string ToString() => _socketAddress?.ToString();
    }
}