using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class Stream : Network.IStreamable
    {
        class Waiter
        {
            public NoWaitValue<int> SyncWait;
            public ArraySegment<byte> Buffer;

        }
        readonly System.Collections.Generic.Queue<Waiter> _ReceiveWaiters;
        private readonly Queue<Waiter> _SendWaiters;

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
        
        private readonly System.Collections.Generic.Queue<BufferSegment> _Sends;
        private readonly System.Collections.Generic.Queue<BufferSegment> _Receives;
        private readonly ArrayPool<byte> _BufferPool;

        public Stream()
        {
            _ReceiveWaiters = new Queue<Waiter>();
            _SendWaiters = new Queue<Waiter>();
            
            _Sends = new System.Collections.Generic.Queue<BufferSegment>();
            _Receives = new System.Collections.Generic.Queue<BufferSegment>();
            _BufferPool = ArrayPool<byte>.Shared;
        }

        public IWaitableValue<int> Push(byte[] buffer, int offset, int count)
        {
            // 租借一个缓冲区
            var pooledBuffer = _BufferPool.Rent(count);
            Buffer.BlockCopy(buffer, offset, pooledBuffer, 0, count);

            // 将缓冲区加入队列
            lock (_Receives)
            {
                _Receives.Enqueue(new BufferSegment(pooledBuffer, 0, count));                
            }
            _ProcessWaiters(_ReceiveWaiters , _Receives);
            
            return new NoWaitValue<int>(count);
        }

        public IWaitableValue<int> Pop(byte[] buffer, int offset, int count)
        {

            return Dequeue(_Sends, buffer, offset, count , _SendWaiters);
        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            // 租借一个缓冲区
            var pooledBuffer = _BufferPool.Rent(count);
            Buffer.BlockCopy(buffer, offset, pooledBuffer, 0, count);

            // 将缓冲区加入队列
            lock (_Sends)
            {
                _Sends.Enqueue(new BufferSegment(pooledBuffer, 0, count));                
            }
            _ProcessWaiters(_SendWaiters , _Sends);
            

            return new NoWaitValue<int>(count);
        }
        
        private bool _ProcessWaiters(Queue<Waiter> waiters, Queue<BufferSegment> buffers)
        {
            lock(waiters)
            {
                lock (buffers)
                {
                    if (waiters.Count == 0 || buffers.Count == 0)
                    {
                        return false;
                    }
                    var waiter = waiters.Dequeue();
                    var count = ProcessQueue(buffers, waiter.Buffer.Array, waiter.Buffer.Offset, waiter.Buffer.Count);
                    waiter.SyncWait.Value.SetValue(count);                    
                    return true;
                }
            }
                
                    
        }

        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {

            return Dequeue(_Receives, buffer, offset, count,  _ReceiveWaiters );
        }

        private IWaitableValue<int> Dequeue(System.Collections.Generic.Queue<BufferSegment> queue, byte[] buffer, int offset, int count, Queue<Waiter> waiters)
        {
            
            var waiter = new Waiter() { SyncWait = new NoWaitValue<int>(), Buffer = new ArraySegment<byte>(buffer, offset, count) };
            lock (waiters)
            {
                waiters.Enqueue(waiter);
            }            
            _ProcessWaiters(waiters, queue);
            return waiter.SyncWait;
        }

        private int ProcessQueue(System.Collections.Generic.Queue<BufferSegment> queue, byte[] buffer, int offset, int count )
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
                    _BufferPool.Return(segment.Buffer);
                }
            }


            return totalRead;
        }
    }
}
