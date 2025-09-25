using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerConnectionManager : IGameLobby , IListenable 
    {
        readonly IdProvider _idProvider;
        readonly System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection> _clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection>();
        readonly NotifiableCollection<IClientConnection> _Connections;
        readonly Notifier<IClientConnection> _clientNotifier;
        

        public GatewayServerConnectionManager()
        {
            _idProvider = new IdProvider();
            _clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection>();
            _Connections = new NotifiableCollection<IClientConnection>();
            _clientNotifier = new Notifier<IClientConnection>(_Connections);
        }

        
        Notifier<IClientConnection> IGameLobby.ClientNotifier => _clientNotifier;

        event Action<IStreamable> _streamableEnterEvent;
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _streamableEnterEvent += value;
            }

            remove
            {
                _streamableEnterEvent -= value;
            }
        }

        event Action<IStreamable> _streamableLeaveEvent;
        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _streamableLeaveEvent += value;
            }

            remove
            {
                _streamableLeaveEvent -= value;
            }
        }

        Value<uint> IGameLobby.Join()
        {
            var id = _idProvider.Landlord.Rent();
            var client = new GatewayServerClientChannel(id);
            if(!_clients.TryAdd(id, client))
            {
                throw new InvalidOperationException("Failed to add new client.");
            }
            _Connections.Items.Add(client);
            
            _streamableEnterEvent?.Invoke(client);
            return id;
        }

        Value<ResponseStatus> IGameLobby.Leave(uint clientId)
        {
            var code = ResponseStatus.NotFound;
            if (_clients.TryRemove(clientId, out var u))
            {
                _Connections.Items.Remove(u);
                
                if (u is IStreamable streamable)
                {
                    _streamableLeaveEvent?.Invoke(streamable);
                }
                code = ResponseStatus.Success;
            }
            return code;
        }
    }
}


