using System;

namespace ClassicUO.Dust765.Lobby.Networking.Collections
{
    public interface IConsolidator
    {
        void Enqueue(byte[] buffer, int offset, int length);
    }

    public abstract class BaseQueue : IConsolidator
    {
        public AsyncCallback Callback { get; set; }

        public abstract Int32 Count { get; }

        public abstract void Clear();

        public abstract ArraySegment<byte> Dequeue(int size);

        public abstract void Enqueue(byte[] buffer, int offset, int length);
    }
}
