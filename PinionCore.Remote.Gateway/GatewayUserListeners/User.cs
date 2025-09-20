using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.GatewayUserListeners 
{
    class User : IUser ,IStreamable
    {
        readonly PinionCore.Network.Stream _Stream;
        readonly Property<uint> _Id;
        public User(uint id)
        {
            _Stream = new PinionCore.Network.Stream();
            _Id = new Property<uint>(id);
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
            throw new NotImplementedException();
        }

        void IUser.Request(byte[] payload)
        {
            _Stream.Push(payload, 0, payload.Length);   
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            var sendBuffer = new byte[count];
            Array.Copy(buffer, offset, sendBuffer, 0, count);
            _ResponseEvent(sendBuffer);
            return count.ToWaitableValue();
        }
    }
}
