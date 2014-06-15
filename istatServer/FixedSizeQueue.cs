using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace istatServer
{
    public class FixedSizeQueue<T> : IEnumerable<T>
    {
        readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        public int MaxSize { get; set; }
        public void Enqueue(T obj)
        {
            _queue.Enqueue(obj);
            lock (this)
            {
                while (_queue.Count > MaxSize) _queue.TryDequeue(out obj);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
