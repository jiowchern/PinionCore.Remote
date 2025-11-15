
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Client
{
}
namespace PinionCore.Remote.Client.Tcp
{
    public class ConnectingEndpoint : IConnectingEndpoint
    {
        public readonly System.Net.IPEndPoint EndPoint;

        readonly PinionCore.Network.Tcp.Connector _Connector;

        public System.Action<SocketError> ConnectFailedEvent;

        System.Action _Dispose;

        public event System.Action BreakEvent;
        public event System.Action<int> ReceiveEvent;
        public event System.Action<int> SendEvent;
        public event Action<SocketError> SocketErrorEvent;

        public ConnectingEndpoint(System.Net.IPEndPoint point)
        {
            _Dispose = () => { };
            _Connector = new Network.Tcp.Connector();            
            EndPoint = point;
        }

        async Task<IStreamable> IConnectingEndpoint.ConnectAsync()
        {
            var result = await _Connector.ConnectAsync(EndPoint).ConfigureAwait(false);
            if (result.Exception != null)
            {
                throw result.Exception;
            }

            var peer = result.Peer ?? throw new InvalidOperationException("Connector returned null peer without exception.");
            peer.SendEvent += SendEvent;
            peer.ReceiveEvent += ReceiveEvent;
            peer.SocketErrorEvent += SocketErrorEvent;
            peer.BreakEvent += BreakEvent;

            _Dispose =async ()=>
            {                
                await peer.Disconnect();
                peer.SendEvent -= SendEvent;
                peer.ReceiveEvent -= ReceiveEvent;
                peer.SocketErrorEvent -= SocketErrorEvent;
                peer.BreakEvent -= BreakEvent;
            };
            return peer;
        }

        void IDisposable.Dispose()
        {
            _Dispose();
        }
    }
    
}
