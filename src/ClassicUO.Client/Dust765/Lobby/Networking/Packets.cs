using System;
using System.IO;
using ClassicUO.Dust765.Managers;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765.Lobby.Networking
{
    internal class PAttack : Packet
    {
        public PAttack(Mobile mob) : base(7)
        {
            base.Stream.Write((uint)mob.Serial);
            base.Stream.Write((string)mob.Name);
        }
    }
    internal class PHiddenPosition : Packet
    {
        public PHiddenPosition(string posXY) : base(6) => base.Stream.Write((string) posXY);
    }
    internal class PTarget : Packet
    {
        public PTarget(uint serial) : base(5) => base.Stream.Write((uint)serial);
    }

    internal class PSetTarget : Packet
    {
        public PSetTarget(Mobile mob) : base(4)
        {
            base.Stream.Write((uint)mob.Serial);
            base.Stream.Write((string)mob.Name);
        } 
    }

    internal class PSpellCast : Packet
    {
        public PSpellCast(SpellAction spell) : base(3) => base.Stream.Write((ushort)spell);
    }

    internal class PDisconnected : Packet
    {
        public PDisconnected(Mobile mob) : base(2)
        {
            base.Stream.Write((uint)mob.Serial);
        }
    }

    internal class PConnected : Packet
    {
        public PConnected(Mobile mob, bool rejoined) : base(1)
        {
            base.Stream.Write((uint)mob.Serial);
            base.Stream.Write((string)mob.Name);
            base.Stream.Write((bool)rejoined);
        }
    }

    public class Packet : IDisposable
    {
        public int ID { get; private set; }

        private PacketWriter _stream;
        public PacketWriter Stream => _stream;

        public Packet(byte packetID)
        {
            this.ID = packetID;

            _stream = new PacketWriter(3);
            _stream.Write(packetID);
            _stream.Write((ushort)0); // length reserved
        }

        public byte[] Compile()
        {
            if(_stream == null)
                throw new ArgumentNullException("stream");

            _stream.Seek(1L, SeekOrigin.Begin);
            _stream.Write((ushort)_stream.Count);
            _stream.Flush();

            return _stream.Compile();
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public override string ToString() => GetType().Name;
    }
}