using System;
using System.Threading;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerClientChannel : IConnection, IStreamable
    {
        readonly PinionCore.Network.Stream _stream;
        readonly PinionCore.Network.IStreamable _Streamable;
        readonly Property<uint> _id;
        int _Hoard;
        Action<byte[]> _responseEvent;
        int _sending;
        public GatewayServerClientChannel(uint id)
        {
            _stream = new PinionCore.Network.Stream();
            _Streamable = _stream;
            _id = new Property<uint>(id);
            
        
        }

        Property<uint> IConnection.Id => _id;

        event Action<byte[]> IConnection.ResponseEvent
        {
            add
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (Interlocked.CompareExchange(ref _responseEvent, value, null) != null)
                {
                    throw new InvalidOperationException("GatewayServerClientChannel.ResponseEvent already registered.");
                }
                _Send();
            }

            remove
            {
                Interlocked.CompareExchange(ref _responseEvent, null, value);
            }
        }

        void IConnection.Request(byte[] payload)
        {
            _stream.Push(payload, 0, payload.Length);
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return _Streamable.Receive(buffer, offset, count);
        }

        

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            var sendTask = _Streamable.Send(buffer, offset, count);
            Interlocked.Add(ref _Hoard, count);
            _Send();
            
            return sendTask;
        }

        private void _Send()
        {
            if (Volatile.Read(ref _responseEvent) == null)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref _sending, 1, 0) != 0)
            {
                return;
            }

            try
            {
                while (true)
                {
                    var handler = Volatile.Read(ref _responseEvent);
                    if (handler == null)
                    {
                        break;
                    }

                    var bytesToSend = Interlocked.Exchange(ref _Hoard, 0);
                    if (bytesToSend == 0)
                    {
                        break;
                    }

                    var buffer = new byte[bytesToSend];
                    var read = WaitForResult(_stream.Pop(buffer, 0, bytesToSend));

                    if (read < 0)
                    {
                        throw new InvalidOperationException("Stream returned a negative read length.");
                    }

                    if (read == 0)
                    {
                        continue;
                    }

                    if (read != bytesToSend)
                    {
                        var remainder = bytesToSend - read;
                        if (remainder > 0)
                        {
                            Interlocked.Add(ref _Hoard, remainder);
                        }

                        if (read != buffer.Length)
                        {
                            var trimmed = new byte[read];
                            Buffer.BlockCopy(buffer, 0, trimmed, 0, read);
                            buffer = trimmed;
                        }
                    }

                    handler = Volatile.Read(ref _responseEvent);
                    if (handler == null)
                    {
                        _stream.Push(buffer, 0, buffer.Length);
                        Interlocked.Add(ref _Hoard, buffer.Length);
                        break;
                    }

                    handler(buffer);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _sending, 0);
                if (Volatile.Read(ref _Hoard) > 0 && Volatile.Read(ref _responseEvent) != null)
                {
                    _Send();
                }
            }
        }

        static int WaitForResult(IAwaitableSource<int> awaitableSource)
        {
            if (awaitableSource == null)
            {
                throw new ArgumentNullException(nameof(awaitableSource));
            }

            var awaiter = awaitableSource.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                return awaiter.GetResult();
            }

            using (var waitHandle = new ManualResetEventSlim(false))
            {
                awaiter.OnCompleted(waitHandle.Set);
                waitHandle.Wait();
            }

            return awaiter.GetResult();
        }
    }
}

