using System;
using System.Collections.Generic;

namespace ClassicUO.Dust765.Lobby.Networking.Collections
{
    public class Collector<T>
    {
        private object _chain = new object();
        private object _item = new object();
        private List<T> _queue = new List<T>();

        public IEnumerable<T> Queue { get { return _queue; } }
        public Int32 Count { get { return _queue.Count; } }

        public T this[int i]
        {
            get { return _queue[i]; }
        }

        ~Collector()
        {
            _chain = null;
            _item = null;
            _queue.Clear();
            _queue = null;
        }

        public void Clear()
        {
            lock (_chain)
            {
                if (_queue == null)
                    _queue = new List<T>();

                _queue.Clear();
            }
        }

        public T Dequeue()
        {
            lock(_chain)
            {
                if(_queue.Count > 0)
                {
                    T item = _queue[0];
                    _queue.RemoveAt(0);
                    return item;
                }
                return default(T);
            }
        }

        public T[] Dequeue(int count)
        {
            lock (_chain)
            {
                if (count > this.Count)
                    count = this.Count;

                T[] array = new T[count];
                for (int i = 0; i < count; ++i)
                {
                    array[i] = _queue[0];
                    _queue.RemoveAt(0);
                }
                return array;
            }
        }

        public void Enqueue(T item)
        {
            lock (_item)
                _queue.Add(item);
        }

        public void Enqueue(T[] items, int count)
        {
            lock (_chain)
            {
                if (count > items.Length)
                    count = items.Length;

                for (int i = 0; i < count; ++i)
                    Enqueue(items[i]);
            }
        }
    }
}
