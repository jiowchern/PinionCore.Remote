using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.GatewayUserListeners 
{
    class User : IUser ,IStreamable
    {
        readonly PinionCore.Network.Stream _Stream;
        readonly Property<uint> _Id;
        readonly uint _UserId;
        public User(uint id)
        {
            _Stream = new PinionCore.Network.Stream();
            _Id = new Property<uint>(id);
            _UserId = id;
        }
        Property<uint> IUser.Id => _Id;

        event Action<byte[]> _ResponseEvent;
        event Action<byte[]> IUser.ResponseEvent
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

        void IUser.Request(byte[] payload)
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
            UserStreamRegistry.Enqueue(_UserId, sendBuffer);
            return sendTask;
        }
    }
}
