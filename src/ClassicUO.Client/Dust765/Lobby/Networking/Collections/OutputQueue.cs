using System;
using System.Collections.Generic;

namespace ClassicUO.Dust765.Lobby.Networking.Collections
{
    public sealed class OutputQueue : BaseQueue, IConsolidator
    {
        public sealed class Gram : IDisposable
        {
            public static Gram Instantiate(byte[] buffer) => new Gram(buffer);

            private byte[] m_Buffer;
            public byte[] Buffer => m_Buffer;
            public int Count => m_Buffer.Length;

            private Gram(byte[] buffer)
            {
                m_Buffer = buffer;
            }

            public void Dispose() => m_Buffer = null;
        }

        private object _chain = new object();
        List<Gram> _queue = new List<Gram>();

        bool _isBusy = false;

        public override int Count => _queue.Count;

        public override void Clear()
        {
            lock (_chain)
            {
                while (_queue.Count > 0)
                {
                    _queue[0].Dispose();
                    _queue.RemoveAt(0);
                }
                _isBusy = false;
            }
        }

        public override ArraySegment<byte> Dequeue(int size) => throw new NotSupportedException();

        private Collector<byte> m_Collector = new Collector<byte>();

        public override void Enqueue(byte[] buffer, int offset, int length)
        {
            lock (_chain)
                _queue.Add(Gram.Instantiate(buffer));
        }

        public Gram Proceed()
        {
            lock (_chain)
            {
                if ( _isBusy )
                {
                    if(_queue.Count > 0)
                        _queue.RemoveAt(0);

                    _isBusy = false;
                    return Proceed();
                }

                _isBusy = true;

                if (_queue.Count > 0)
                    return _queue[0];

                _isBusy = false;
                return null;
            }
        }

        public Gram Query()
        {
            lock (_chain)
            {
                if (_isBusy || (_queue.Count <= 0))
                    return null;

                _isBusy = true;

                return _queue[0];
            }
        }
    }
}
