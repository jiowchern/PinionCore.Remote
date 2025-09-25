using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class SessionAdapter : IStreamable ,IDisposable
    {
        readonly IClientConnection _client;
        readonly PinionCore.Network.BufferRelay _bufferRelay;
        public SessionAdapter(IClientConnection client)
        {
            _bufferRelay = new PinionCore.Network.BufferRelay();
            _client.ResponseEvent += _client_ResponseEvent;
        }

        private void _client_ResponseEvent(byte[] obj)
        {
            _bufferRelay.Push(obj, 0, obj.Length);
        }

        void IDisposable.Dispose()
        {
            _client.ResponseEvent -= _client_ResponseEvent;
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return _bufferRelay.Pop(buffer, offset, count);
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            var buf = new byte[count];
            Array.Copy(buffer, offset, buf, 0, count);
            _client.Request(buf);
            return count.ToWaitableValue();
        }
    }
}



