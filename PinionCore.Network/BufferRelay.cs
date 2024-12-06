using System;
using System.Collections.Generic;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class BufferRelay : IStreamable
    {
        private class BufferSegment
        {
            public byte[] Buffer { get; }
            public int Offset { get; private set; }
            public int Length { get; private set; }

            public BufferSegment(byte[] buffer, int offset, int length)
            {
                Buffer = buffer;
                Offset = offset;
                Length = length;
            }

            public void Advance(int count)
            {
                Offset += count;
                Length -= count;
            }
        }

        class Waiter
        {
            public NoWaitValue<int> SyncWait;
            public ArraySegment<byte> Buffer;

        }
        readonly System.Collections.Generic.Queue<Waiter> _Waiters;
        private readonly System.Collections.Generic.Queue<BufferSegment> _Segments;


        
        public BufferRelay()
        {
            _Waiters = new Queue<Waiter>();
            _Segments = new System.Collections.Generic.Queue<BufferSegment>();
        }

        public IWaitableValue<int> Push(byte[] buffer, int offset, int count)
        {
            lock (_Segments)
            {
                _Segments.Enqueue(new BufferSegment(buffer, offset, count));
            }
            _ProcessWaiters(_Waiters, _Segments);
            return new NoWaitValue<int>(count);
        }

        public IWaitableValue<int> Pop(byte[] buffer, int offset, int count)
        {
            var waiter = new Waiter() { SyncWait = new NoWaitValue<int>(), Buffer = new ArraySegment<byte>(buffer, offset, count) };
            lock (_Waiters)
            {
                _Waiters.Enqueue(waiter);
            }            
            _ProcessWaiters(_Waiters, _Segments);
            return waiter.SyncWait;
        }
        public bool HasPendingSegments()
        {
            lock (_Segments)
            {
                return _Segments.Count > 0;
            }
        }
        private bool _ProcessWaiters(Queue<Waiter> waiters, Queue<BufferSegment> buffers)
        {
            lock (waiters)
            {
                lock (buffers)
                {
                    if (waiters.Count == 0 || buffers.Count == 0)
                    {
                        return false;
                    }
                    var waiter = waiters.Dequeue();
                    var count = _ProcessQueue(buffers, waiter.Buffer.Array, waiter.Buffer.Offset, waiter.Buffer.Count);
                    waiter.SyncWait.Value.SetValue(count);
                    return true;
                }
            }


        }

        private int _ProcessQueue(System.Collections.Generic.Queue<BufferSegment> queue, byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count && queue.Count > 0)
            {
                BufferSegment segment = queue.Peek();
                var bytesToCopy = Math.Min(segment.Length, count - totalRead);

                // 使用 Span 进行高效的内存复制
                var sourceSpan = new ReadOnlySpan<byte>(segment.Buffer, segment.Offset, bytesToCopy);
                var destinationSpan = new Span<byte>(buffer, offset + totalRead, bytesToCopy);
                sourceSpan.CopyTo(destinationSpan);

                totalRead += bytesToCopy;
                segment.Advance(bytesToCopy);

                if (segment.Length == 0)
                {
                    // 从队列中移除已消费的缓冲区并归还到池中
                    queue.Dequeue();
                    //_BufferPool.Return(segment.Buffer);
                }
            }


            return totalRead;
        }

        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return Pop(buffer, offset, count);
        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            return Push(buffer, offset, count);
        }
    }
}
