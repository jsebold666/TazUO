using System.Collections.Generic;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public delegate void OnPacketReceive(NetState ns, PacketReader pvSrc);

    public class PacketHandler
    {
        public const int MAX_CAPACITY = 256;

        private Dictionary<int, PacketHandler> _handlers = new Dictionary<int, PacketHandler>();

        public bool HasChildren => _handlers.Count > 0;

        private string _stringID;
        private int _packetID;

        public string Name => _stringID;
        public int PacketID => _packetID;

        public OnPacketReceive Callback { get; private set; }

        public PacketHandler(int packetID, OnPacketReceive receive)
        {
            _stringID = receive.Method.Name;
            _packetID = packetID;

            this.Callback = receive;
        }

        public PacketHandler this[int packetID] 
        {
            get
            { 
                if ( _handlers.ContainsKey(packetID)){
                    return _handlers[packetID];
                } else {
                    return null;
                }
            }
            set
            {
                if (_handlers.ContainsKey(packetID))
                {
                    if (value == null)
                    {
                        _handlers.Remove(packetID);
                        return;
                    }
                    _handlers[packetID] = value;
                }
                else if (value != null)
                {
                    _handlers.Add(packetID, value);
                }
            }
        }



        public override string ToString()
        {
            return _stringID;
        }
    }
}
