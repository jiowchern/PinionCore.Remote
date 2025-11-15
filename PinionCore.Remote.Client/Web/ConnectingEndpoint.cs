using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Client.Web
{
    public sealed class ConnectingEndpoint : IConnectingEndpoint
    {
        private readonly string _address;
        private readonly Func<ClientWebSocket> _socketFactory;
        private Action _dispose;

        public event Action BreakEvent;
        public event Action<WebSocketState> WebSocketErrorEvent;

        public ConnectingEndpoint(string address)
            : this(address, () => new ClientWebSocket())
        {
        }

        internal ConnectingEndpoint(string address, Func<ClientWebSocket> socketFactory)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
            _dispose = () => { };
        }

        async Task<IStreamable> IConnectingEndpoint.ConnectAsync()
        {
            var socket = _socketFactory();
            PinionCore.Network.Web.Peer peer = null;
            try
            {
                var connector = new PinionCore.Network.Web.Connecter(socket);
                peer = await connector.ConnectAsync(_address).ConfigureAwait(false);
                if (peer == null)
                {
                    throw new InvalidOperationException($"Failed to establish WebSocket connection to '{_address}'.");
                }

                peer.ErrorEvent += HandlePeerError;
                _dispose = () =>
                {
                    peer.ErrorEvent -= HandlePeerError;
                    peer.DisconnectAsync().GetAwaiter().GetResult();
                    ((IDisposable)peer).Dispose();
                    socket.Dispose();
                };

                return peer;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private void HandlePeerError(WebSocketState state)
        {
            WebSocketErrorEvent?.Invoke(state);
            BreakEvent?.Invoke();
        }

        void IDisposable.Dispose()
        {
            _dispose();
        }
    }
}
