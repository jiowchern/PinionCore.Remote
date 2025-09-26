using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using static PinionCore.Network.Tcp.SockerTransactor;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerClientChannel : IClientConnection ,IStreamable
    {
        readonly PinionCore.Network.Stream _stream;
        readonly PinionCore.Network.IStreamable _Streamable;
        readonly Property<uint> _id;
        int _Hoard;
        public GatewayServerClientChannel(uint id)
        {
            _stream = new PinionCore.Network.Stream();
            _Streamable = _stream;
            _id = new Property<uint>(id);
            
        
        }

        

        Property<uint> IClientConnection.Id => _id;

        event Action<byte[]> _responseEvent;
        event Action<byte[]> IClientConnection.ResponseEvent
        {
            add
            {
                _Send(value);
                _responseEvent += value;                
            }

            remove
            {
                _responseEvent -= value;
                
            }
        }

        void IClientConnection.Request(byte[] payload)
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
            System.Threading.Interlocked.Add(ref _Hoard, count);
            
            if (_responseEvent!= null)
            {
                _Send(_responseEvent);
            }
            return sendTask;
        }

        private void _Send(Action<byte[]> action)
        {
            var buf = new byte[_Hoard];
            System.Threading.Interlocked.Exchange(ref _Hoard, 0);            
            _stream.Pop(buf, 0, buf.Length);
            action(buf);
        }
    }
}

