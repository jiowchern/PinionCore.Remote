using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerConnectionPool : IConnectionProvider , IListenable 
    {
        readonly IdProvider _idProvider;
        readonly System.Collections.Concurrent.ConcurrentDictionary<uint, GatewayServerClientChannel> _clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, GatewayServerClientChannel>();
        readonly NotifiableCollection<IConnection> _Connections;
        readonly Notifier<IConnection> _clientNotifier;        

        public GatewayServerConnectionPool()
        {
            _idProvider = new IdProvider();
            _clients = new System.Collections.Concurrent.ConcurrentDictionary<uint, GatewayServerClientChannel>();
            _Connections = new NotifiableCollection<IConnection>();
            _clientNotifier = new Notifier<IConnection>(_Connections);
        }

        
        Notifier<IConnection> IConnectionProvider.ConnectionNotifier => _clientNotifier;

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

        Value<uint> IConnectionProvider.Join()
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

        Value<ResponseStatus> IConnectionProvider.Leave(uint clientId)
        {
            var code = ResponseStatus.NotFound;
            if (_clients.TryRemove(clientId, out var u))
            {
                _Connections.Items.Remove(u);
                _idProvider.Landlord.Return(clientId);
                _streamableLeaveEvent?.Invoke(u);
                
                code = ResponseStatus.Success;
            }
            return code;
        }
    }
}


