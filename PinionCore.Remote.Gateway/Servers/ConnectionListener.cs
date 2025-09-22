using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class ConnectionListener : IGameLobby , IListenable 
    {
        readonly IdProvider _IdProvider;
        readonly System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection> _Clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection>();
        readonly Notifier<IClientConnection> _ClientNotifier;
        

        public ConnectionListener()
        {
            _IdProvider = new IdProvider();
            _Clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, IClientConnection>();
            _ClientNotifier = new Notifier<IClientConnection>();
        }

        
        Notifier<IClientConnection> IGameLobby.ClientNotifier => _ClientNotifier;

        event Action<IStreamable> _StreamableEnterEvent;
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _StreamableEnterEvent += value;
            }

            remove
            {
                _StreamableEnterEvent -= value;
            }
        }

        event Action<IStreamable> _StreamableLeaveEvent;
        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _StreamableLeaveEvent += value;
            }

            remove
            {
                _StreamableLeaveEvent -= value;
            }
        }

        Value<uint> IGameLobby.Join()
        {
            var id = _IdProvider.Landlord.Rent();
            var client = new ConnectedClient(id);
            if(!_Clients.TryAdd(id, client))
            {
                throw new InvalidOperationException("Failed to add new client.");
            }
            _ClientNotifier.Collection.Add(client);
            ClientStreamRegistry.Register(id, client);
            _StreamableEnterEvent?.Invoke(client);
            return id;
        }

        Value<ResponseStatus> IGameLobby.Leave(uint clientId)
        {
            var code = ResponseStatus.NotFound;
            if (_Clients.TryRemove(clientId, out var u))
            {
                _ClientNotifier.Collection.Remove(u);
                ClientStreamRegistry.Unregister(clientId);
                if (u is IStreamable streamable)
                {
                    _StreamableLeaveEvent?.Invoke(streamable);
                }
                code = ResponseStatus.Success;
            }
            return code;
        }
    }
}
