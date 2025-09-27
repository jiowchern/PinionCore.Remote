using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class SessionAdapter : IStreamable ,IDisposable
    {
        readonly IConnection _client;
        readonly PinionCore.Network.BufferRelay _bufferRelay;
        readonly PinionCore.Network.IStreamable _Streamable;
        public SessionAdapter(IConnection client)
        {
            _bufferRelay = new PinionCore.Network.BufferRelay();
            _client = client;
            _Streamable = _bufferRelay;
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
            
            return _Streamable.Receive(buffer, offset, count);
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



