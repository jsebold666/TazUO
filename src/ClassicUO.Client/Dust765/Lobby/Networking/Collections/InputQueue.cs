using System;

namespace ClassicUO.Dust765.Lobby.Networking.Collections
{
    public sealed class InputQueue : BaseQueue, IConsolidator
    {
        private Collector<byte> m_Buffer = new Collector<byte>();

        public override Int32 Count => m_Buffer.Count;

        public override void Clear() => m_Buffer.Clear();

        public void ClearByte()
        {
            if(m_Buffer.Count >= 0)
                m_Buffer.Dequeue();
        }

        public override ArraySegment<byte> Dequeue(int size)
        {
            if(size <= 0)
                size = 0;

            return new ArraySegment<byte>(m_Buffer.Dequeue(size));
        }

        public override void Enqueue(byte[] buffer, int offset, int length) => m_Buffer.Enqueue(buffer, length);

        public int GetPacketID() => m_Buffer.Count >= 1 ? m_Buffer[0] : -1;
        public int GetPacketLength() => m_Buffer.Count >= 3 ? (short)( m_Buffer[1 % m_Buffer.Count] << 8 | m_Buffer[2 % m_Buffer.Count] ) : -1;
    }
}
