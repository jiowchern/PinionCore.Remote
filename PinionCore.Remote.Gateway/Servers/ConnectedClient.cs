using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers 
{
    class ConnectedClient : IClientConnection ,IStreamable
    {
        readonly PinionCore.Network.Stream _Stream;
        readonly Property<uint> _Id;
        readonly uint _ConnectedClientId;
        public ConnectedClient(uint id)
        {
            _Stream = new PinionCore.Network.Stream();
            _Id = new Property<uint>(id);
            _ConnectedClientId = id;
        }
        Property<uint> IClientConnection.Id => _Id;

        event Action<byte[]> _ResponseEvent;
        event Action<byte[]> IClientConnection.ResponseEvent
        {
            add
            {
                _ResponseEvent += value;
            }

            remove
            {
                _ResponseEvent -= value;
            }
        }

        

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return ((IStreamable)_Stream).Receive(buffer, offset, count);
        }

        void IClientConnection.Request(byte[] payload)
        {
            _Stream.Push(payload, 0, payload.Length);   
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            var sendTask = ((IStreamable)_Stream).Send(buffer, offset, count);
            var sendBuffer = new byte[count];
            Array.Copy(buffer, offset, sendBuffer, 0, count);
            var response = _ResponseEvent;
            if (response != null)
            {
                response(sendBuffer);
            }
            ClientStreamRegistry.Enqueue(_ConnectedClientId, sendBuffer);
            return sendTask;
        }
    }
}
