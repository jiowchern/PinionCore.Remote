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

        public GatewayServerClientChannel(uint id)
        {
            _stream = new PinionCore.Network.Stream();
            _Streamable = _stream;
            _id = new Property<uint>(id);
        }

        Property<uint> IConnection.Id => _id;

        public PinionCore.Network.IStreamable GetReverseView()
        {
            return new PinionCore.Network.ReverseStream(_stream);
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return _Streamable.Receive(buffer, offset, count);
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            return _Streamable.Send(buffer, offset, count);
        }
    }
}

