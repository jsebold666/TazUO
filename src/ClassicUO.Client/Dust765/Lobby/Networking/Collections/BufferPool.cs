using System;
using System.Collections;

namespace ClassicUO.Dust765.Lobby.Networking.Collections
{
    public sealed class BufferPool
    {
        public static readonly BufferPool Instance = new BufferPool( 512, 2048 );

        private Int32 _bufferSize;

        private Queue _pool;
        private Int32 _poolCapacity;

        public BufferPool( int capacity, int bufferSize )
        {
            _bufferSize = bufferSize;

            _pool = new Queue( _poolCapacity = capacity );
            for ( int i = 0; i < _poolCapacity; i++ )
                _pool.Enqueue( new byte[bufferSize] );
        }

        public byte[] AcquireBuffer()
        {
            lock( _pool )
            {
                if ( _pool.Count > 0 )
                    return (byte[])_pool.Dequeue();
            }
            return new byte[_bufferSize];
        }

        public void Enqueue( byte[] buffer )
        {
            lock( _pool )
                _pool.Enqueue( buffer );
        }

        public int BufferSize => _bufferSize;
        public int Capacity => _poolCapacity;
    }
}
