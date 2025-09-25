using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerClientChannel : IClientConnection ,IStreamable
    {
        readonly PinionCore.Network.Stream _stream;
        readonly Property<uint> _id;
        readonly uint _channelId;
        public GatewayServerClientChannel(uint id)
        {
            _stream = new PinionCore.Network.Stream();
            _id = new Property<uint>(id);
            _channelId = id;
        }
        Property<uint> IClientConnection.Id => _id;

        event Action<byte[]> _responseEvent;
        event Action<byte[]> IClientConnection.ResponseEvent
        {
            add
            {
                _responseEvent += value;
            }

            remove
            {
                _responseEvent -= value;
            }
        }

        

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return ((IStreamable)_stream).Receive(buffer, offset, count);
        }

        void IClientConnection.Request(byte[] payload)
        {
            _stream.Push(payload, 0, payload.Length);   
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            var sendTask = ((IStreamable)_stream).Send(buffer, offset, count);
            var sendBuffer = new byte[count];
            Array.Copy(buffer, offset, sendBuffer, 0, count);
            var response = _responseEvent;
            if (response != null)
            {
                response(sendBuffer);
            }
            
            return sendTask;
        }
    }
}

