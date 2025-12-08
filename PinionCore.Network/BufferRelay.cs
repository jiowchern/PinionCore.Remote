using System;
using System.Collections.Generic;
using System.Threading;
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
            public CancellationToken Cancel;
            public NoWaitValue<int> SyncWait;
            public ArraySegment<byte> Buffer;

        }
        readonly System.Collections.Generic.Queue<Waiter> _Waiters;

        class SegmentedBuffer
        {
            public BufferSegment Buffer;
            public CancellationToken Cancel;
        }

        private readonly System.Collections.Generic.Queue<SegmentedBuffer> _Segments;


        public BufferRelay()
        {


            _Waiters = new Queue<Waiter>();
            _Segments = new System.Collections.Generic.Queue<SegmentedBuffer>();


        }

        public IAwaitableSource<int> Push(byte[] buffer, int offset, int count,CancellationToken cancellation)
        {
            lock (_Segments)
            {
                _Segments.Enqueue(new SegmentedBuffer()
                {
                    Buffer = new BufferSegment(buffer, offset, count),
                    Cancel = cancellation
                });
            }
            _Digestion();
            return new NoWaitValue<int>(count);
        }

        public IAwaitableSource<int> Pop(byte[] buffer, int offset, int count,CancellationToken cancellation)
        {
            var waiter = new Waiter() { SyncWait = new NoWaitValue<int>(), Buffer = new ArraySegment<byte>(buffer, offset, count) ,Cancel = cancellation};
            lock (_Waiters)
            {
                _Waiters.Enqueue(waiter);
            }
            _Digestion();
            return waiter.SyncWait;
        }
        public bool HasPendingSegments()
        {
            lock (_Segments)
            {
                return _Segments.Count > 0;
            }
        }

        void _Digestion()
        {
            Tuple<Waiter, int> waiter = _ProcessWaiters(_Waiters, _Segments);
            if (waiter == null)
            {
                return;
            }
            waiter.Item1.SyncWait.SetValue(waiter.Item2);
        }
        private Tuple<Waiter, int> _ProcessWaiters(Queue<Waiter> waiters, Queue<SegmentedBuffer> buffers)
        {
            lock (waiters)
            {
                lock (buffers)
                {
                    if (waiters.Count == 0 || buffers.Count == 0)
                    {
                        return null;
                    }

                    Waiter waiter = waiters.Dequeue();
                    if(waiter.Cancel.IsCancellationRequested)
                    {
                        return new Tuple<Waiter, int>(waiter, 0);
                    }

                    var count = _ProcessQueue(buffers, waiter.Buffer.Array, waiter.Buffer.Offset, waiter.Buffer.Count);
                    return new Tuple<Waiter, int>(waiter, count);
                }
            }


        }


        private int _ProcessQueue(System.Collections.Generic.Queue<SegmentedBuffer> queue, byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count && queue.Count > 0)
            {
                var segBuf = queue.Peek();
                BufferSegment segment = segBuf.Buffer;
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

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return Pop(buffer, offset, count, token);
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return Push(buffer, offset, count, token);
        }

        void IDisposable.Dispose()
        {
            // BufferRelay 是內存中的緩衝區，由 GC 自動回收
        }
    }
}
